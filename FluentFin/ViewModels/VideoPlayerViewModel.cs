using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Web;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using FluentFin.Contracts.Services;
using FluentFin.Contracts.ViewModels;
using FluentFin.Core.Contracts.Services;
using FluentFin.Core.Services;
using FluentFin.Core.Settings;
using FluentFin.Core.ViewModels;
using FluentFin.Helpers;
using FluentFin.Services;
using Flurl;
using Jellyfin.Sdk.Generated.Models;
using Jellyfin.Sdk.WebSockets;
using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace FluentFin.ViewModels;

public partial class VideoPlayerViewModel(IJellyfinClient jellyfinClient,
							TrickplayViewModel trickplayViewModel,
							ILogger<VideoPlayerViewModel> logger,
							IObservable<IOutboundWebSocketMessage> webSocketMessages,
							INavigationService navigationService,
							IPlaybackSessionManager playbackSessions,
							ISettings settings,
							ITaskBarProgress taskBarProgress) : ObservableObject, INavigationAware
{
	private readonly CompositeDisposable _disposables = [];
	private PlaybackSession? _session;
	private int _loadVersion;
	private KeyboardMediaPlayerController? _keyboardController;
	private PlayQueueUpdate? _playQueueUpdate;
	private TimeSpan _duration;
	private SendCommand? _previousCommand;


	public TrickplayViewModel TrickplayViewModel { get; } = trickplayViewModel;
	public IJellyfinClient JellyfinClient { get; } = jellyfinClient;


	[ObservableProperty]
	public partial IMediaPlayerController? MediaPlayer { get; set; }

	[ObservableProperty]
	public partial List<MediaSegmentDto> Segments { get; set; } = [];

	[ObservableProperty]
	public partial bool IsSkipButtonVisible { get; set; }

	[ObservableProperty]
	public partial PlaylistViewModel Playlist { get; set; } = new PlaylistViewModel();

	[ObservableProperty]
	public partial MediaPlayerType MediaPlayerType { get; set; }

	public PlaybackProgressInfo_PlayMethod PlayMethod { get; private set; }

	public Action? ToggleFullScreen { get; set; }

	public Action<int>? SetProgress { get; set; }

	public async Task OnNavigatedFrom()
	{
		// invalidates any media that is still being loaded
		Interlocked.Increment(ref _loadVersion);
		Playlist.PropertyChanged -= OnPlaylistPropertyChanged;

		_disposables.Dispose();
		_keyboardController?.UnsubscribeEvents();

		NativeMethods.AllowSleep();

		taskBarProgress.Clear();
		await StopPlayback();
	}

	/// <summary>
	/// Releases the player's media source and reports the current playback session as stopped, which makes the server
	/// end its transcoding job. Safe to call repeatedly, only the first call for a session has an effect.
	/// </summary>
	private async Task StopPlayback(bool stopPlayer = true)
	{
		var session = Interlocked.Exchange(ref _session, null);
		long? position = null;

		try
		{
			// the position is lost once the player is stopped
			position = GetKnownPosition();

			if (stopPlayer)
			{
				MediaPlayer?.Stop();
			}
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Failed to stop the media player");
		}

		await playbackSessions.StopAsync(session, position);
	}

	public async Task OnNavigatedTo(object parameter)
	{
		MediaPlayerType = settings.MediaPlayer;

		if (parameter is BaseItemDto { Id: { } id })
		{
			await Initialize(id);
		}
		else if (parameter is PlayQueueUpdate pqu)
		{
			_playQueueUpdate = pqu;
			InitializeForSyncPlay(pqu);
		}

		if (Playlist.Items.Count == 0)
		{
			return;
		}

		this.WhenAnyValue(x => x.MediaPlayer).WhereNotNull().Subscribe(SubscribeEvents);
		SubscribeWebsocketMessage();
		NativeMethods.PreventSleep();

		Playlist.PropertyChanged += OnPlaylistPropertyChanged;

		await TrickplayViewModel.Initialize();

		this.WhenAnyValue(x => x.MediaPlayer)
			.WhereNotNull()
			.Subscribe(mp =>
			{
				_keyboardController?.UnsubscribeEvents();
				_keyboardController = new KeyboardMediaPlayerController(mp, JellyfinClient, Skip, ToggleFullScreen);

				if (_playQueueUpdate is null)
				{
					Playlist.AutoSelect();
				}
				else
				{
					Playlist.SelectedItem = Playlist.Items.FirstOrDefault();
				}
			});

		Observable.Timer(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(20))
			.SelectMany(_ => UpdateStatus().ToObservable())
			.Subscribe()
			.DisposeWith(_disposables);
	}


	private async Task Initialize(Guid? id)
	{
		if (!id.HasValue)
		{
			return;
		}

		var dto = await JellyfinClient.GetItem(id.Value);

		if (dto is null)
		{
			return;
		}

		Playlist = dto.Type switch
		{
			BaseItemDto_Type.Movie => PlaylistViewModel.FromMovie(dto),
			BaseItemDto_Type.Episode => await PlaylistViewModel.FromEpisode(JellyfinClient, dto),
			BaseItemDto_Type.Series => await PlaylistViewModel.FromSeries(JellyfinClient, dto),
			BaseItemDto_Type.Season => await PlaylistViewModel.FromSeason(JellyfinClient, dto),
			_ => new PlaylistViewModel()
		};
	}

	private void InitializeForSyncPlay(PlayQueueUpdate pqu)
	{
		if (pqu.PlayingItemIndex is not >= 0)
		{
			return;
		}

		Playlist = PlaylistViewModel.FromSyncPlay(pqu.Playlist ?? []);
	}

	[RelayCommand]
	private async Task Skip()
	{
		if (MediaPlayer is null)
		{
			return;
		}

		var currentTime = MediaPlayer.Position.Ticks;
		var segment = Segments.FirstOrDefault(x => currentTime > x.StartTicks && currentTime < x.EndTicks);

		if (segment is not { EndTicks: not null })
		{
			return;
		}

		var endPosition = TimeSpan.FromTicks(segment.EndTicks.Value);

		if (_playQueueUpdate is not null)
		{
			await JellyfinClient.SignalSeekForSyncPlay(endPosition);
		}

		MediaPlayer.SeekTo(endPosition);
	}

	private async void OnPlaylistPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
	{
		if (MediaPlayer is null)
		{
			return;
		}

		if (e.PropertyName != nameof(Playlist.SelectedItem))
		{
			return;
		}

		if (Playlist.SelectedItem is not { Dto.Id: not null } selectedItem)
		{
			return;
		}

		var version = Interlocked.Increment(ref _loadVersion);

		// clean up the previous session first, whether or not the new media can be loaded
		await StopPlayback();

		var full = await JellyfinClient.GetItem(selectedItem.Dto.Id.Value);

		if (full is null || version != _loadVersion)
		{
			return;
		}

		var mediaResponse = await GetMediaUrl(full);

		if (mediaResponse is null || version != _loadVersion)
		{
			return;
		}

		TrickplayViewModel.SetItem(full);

		var defaultSubtitleIndex = mediaResponse.MediaSourceInfo.DefaultSubtitleStreamIndex;
		selectedItem.Media = mediaResponse;

		// registered before playback starts so that leaving the page at any moment can clean it up
		var session = playbackSessions.Begin(selectedItem.Dto.Id.Value, mediaResponse);
		session.PositionProvider = GetKnownPosition;
		_session = session;

		var success = MediaPlayer.Play(mediaResponse.Uri, mediaResponse.MediaSourceInfo.DefaultAudioStreamIndex ?? 0);
		if (!success)
		{
			logger.LogError("Unable to open media from {URL}", mediaResponse.Uri);
			await StopPlayback();
			return;
		}

		PlayMethod = mediaResponse.PlayMethod;

		if (selectedItem.Dto.UserData?.PlaybackPositionTicks is { } ticks)
		{
			MediaPlayer.SeekTo(TimeSpan.FromTicks(ticks));
		}

		if (mediaResponse.MediaSourceInfo.MediaStreams?.FirstOrDefault(x => x.Index == defaultSubtitleIndex) is { } subtitleStream)
		{
			OpenSubtitles(MediaPlayer, mediaResponse, subtitleStream);
		}

		await playbackSessions.ReportStartAsync(session, GetPlaybackState(MediaPlayer));
	}

	private void OpenSubtitles(IMediaPlayerController mp, MediaResponse response, MediaStream stream)
	{
		if (stream is null)
		{
			return;
		}

		var subtitles = response?.MediaSourceInfo.MediaStreams?.Where(x => x.Type == MediaStream_Type.Subtitle).ToList() ?? [];

		try
		{
			if (stream.IsExternal == true)
			{
				var url = HttpUtility.UrlDecode(JellyfinClient.BaseUrl.AppendPathSegment(stream.DeliveryUrl).ToString());
				mp.OpenExternalSubtitleTrack(url);
			}
			else
			{
				var subtitleIndex = subtitles.Where(x => x.IsExternal is false).IndexOf(stream);
				if (stream.Index is not { } trackIndex)
				{
					return;
				}

				mp.OpenInternalSubtitleTrack(trackIndex, subtitleIndex);
			}
		}
		catch { }
	}


	private async Task<MediaResponse?> GetMediaUrl(BaseItemDto dto)
	{
		var segments = await JellyfinClient.GetMediaSegments(dto, [MediaSegmentType.Intro, MediaSegmentType.Outro]);

		if (segments is { Items: not null })
		{
			Segments = segments.Items;
		}

		return await JellyfinClient.GetMediaUrl(dto);
	}

	// some backends keep reporting the position of the previous media until the new one is playing,
	// so the position only belongs to the current session once the player is actually playing it
	// a backend can also report Ended when it failed to open the media, there the position is zero and not a real position
	private long? GetKnownPosition() => MediaPlayer switch
	{
		{ State: MediaPlayerState.Playing or MediaPlayerState.Paused } mp => mp.Position.Ticks,
		{ State: MediaPlayerState.Ended, Position.Ticks: > 0 } mp => mp.Position.Ticks,
		_ => null,
	};

	private PlaybackState GetPlaybackState(IMediaPlayerController mp) => new(
		mp.Position.Ticks,
		!mp.IsPlaying,
		mp.IsMuted,
		mp.AudioTrackIndex,
		mp.SubtitleTrackIndex);

	private async Task UpdateStatus()
	{
		if (MediaPlayer is null || _session is not { } session)
		{
			return;
		}

		if (MediaPlayer.Position.Ticks == 0)
		{
			return;
		}

		if (MediaPlayer.State is MediaPlayerState.Error or MediaPlayerState.Ended or MediaPlayerState.Opening)
		{
			return;
		}

		taskBarProgress.SetProgressPercent((int)((MediaPlayer.Position.TotalSeconds / _duration.TotalSeconds) * 100));

		await playbackSessions.ReportProgressAsync(session, GetPlaybackState(MediaPlayer));
	}

	private void SubscribeEvents(IMediaPlayerController mp)
	{
		mp.Playing.Where(_ => _disposables.IsDisposed).Subscribe(_ => mp.Stop());
		// the backends raise these on their own native threads, stopping a player from there can deadlock
		mp.Ended.ObserveOn(RxSchedulers.MainThreadScheduler).Subscribe(async _ =>
		{
			if (Playlist.CanSelectNext)
			{
				// the previous session is stopped when the next item is selected
				taskBarProgress.Clear();
				Playlist.SelectNext();
			}
			else
			{
				await StopPlayback(stopPlayer: false);
			}
		});
		// events of a previous media can arrive late, only act on them if the player is still in that state
		mp.Stopped.ObserveOn(RxSchedulers.MainThreadScheduler).Where(_ => mp.State is MediaPlayerState.Stopped).Subscribe(async _ => await StopPlayback(stopPlayer: false));
		mp.Errored.ObserveOn(RxSchedulers.MainThreadScheduler).Subscribe(async _ =>
		{
			logger.LogError("An error occurred while playing media");
			await StopPlayback();
		});
		mp.PositionChanged
			.Where(_ => MediaPlayer?.State == MediaPlayerState.Playing)
			.Select(x => x.Ticks)
			.Select(ticks => Segments.Any(segment => ticks > segment.StartTicks && ticks < segment.EndTicks))
			.DistinctUntilChanged()
			.ObserveOn(RxSchedulers.MainThreadScheduler)
			.Subscribe(isVisible => IsSkipButtonVisible = isVisible);
		mp.DurationChanged.Subscribe(d => _duration = d);

		mp.MediaLoaded
		  .Where(_ => _playQueueUpdate is not null)
		  .SelectMany(_ => JellyfinClient.SignalReadyForSyncPlay(new ReadyRequestDto
		  {
			  When = TimeProvider.System.GetUtcNow(),
			  IsPlaying = mp.IsPlaying,
			  PlaylistItemId = _playQueueUpdate!.Playlist![_playQueueUpdate.PlayingItemIndex ?? 0].PlaylistItemId,
			  PositionTicks = mp.Position.Ticks
		  }).ToObservable())
		  .Subscribe();
	}

	private void SubscribeWebsocketMessage()
	{
		webSocketMessages
			.ObserveOn(RxSchedulers.MainThreadScheduler)
			.Subscribe(async message =>
			{
				if (MediaPlayer is null)
				{
					return;
				}

				switch (message)
				{
					case PlaystateMessage { Data.Command: PlaystateRequest_Command.PlayPause }:
						await MediaPlayer.TogglePlayPlause(JellyfinClient);
						await UpdateStatus();
						break;
					case PlaystateMessage { Data.Command: PlaystateRequest_Command.Stop }:
						await StopPlayback();
						navigationService.NavigateTo<HomeViewModel>(new());
						break;
					case SyncPlayCommandMessage { Data: not null } syncPlay:

						if (syncPlay.Data.When == _previousCommand?.When)
						{
							return;
						}

						switch (syncPlay.Data.Command)
						{
							case SendCommand_Command.Pause:
								await SchedulePause(syncPlay.Data, MediaPlayer);
								break;
							case SendCommand_Command.Unpause:
								await SchedulePlay(syncPlay.Data, MediaPlayer);
								break;
							case SendCommand_Command.Seek:
								await SchedulePause(syncPlay.Data, MediaPlayer);
								break;
							case SendCommand_Command.Stop:
								await StopPlayback();
								break;
						}

						_previousCommand = syncPlay.Data;
						break;
				}
			})
			.DisposeWith(_disposables);
	}

	private async Task SchedulePause(SendCommand command, IMediaPlayerController mp)
	{
		if (command is not { When: { } commandTime, PositionTicks: { } positionTicks })
		{
			return;
		}

		var currentTime = await JellyfinClient.SyncTime();

		mp.SeekTo(new TimeSpan(positionTicks));
		if (commandTime > currentTime)
		{
			await Task.Delay(commandTime - currentTime);
			mp.Pause();
		}
		else
		{
			mp.Pause();
		}
	}

	private async Task SchedulePlay(SendCommand command, IMediaPlayerController mp)
	{
		if (command is not { When: { } commandTime, PositionTicks: { } positionTicks })
		{
			return;
		}

		// the play session was reported stopped, playing would restart its stream without being tracked,
		// after a stop the group restarts playback with a new play queue, which opens a new session
		if (_session is null)
		{
			return;
		}

		var currentTime = await JellyfinClient.SyncTime();

		if (commandTime > currentTime)
		{
			mp.SeekTo(new TimeSpan(positionTicks));
			await Task.Delay(commandTime - currentTime);
			mp.Play();
		}
		else
		{
			var serverPosition = positionTicks + (currentTime - commandTime).Ticks;
			mp.SeekTo(new TimeSpan(serverPosition));
			mp.Play();
		}
	}
}

