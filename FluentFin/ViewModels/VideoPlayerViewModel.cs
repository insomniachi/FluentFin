using System.Reactive.Disposables;
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
using FluentFin.Core.WebSockets;
using FluentFin.Core.WebSockets.Messages;
using FluentFin.Helpers;
using FluentFin.Services;
using Flurl;
using Jellyfin.Sdk.Generated.Models;
using Microsoft.Extensions.Logging;
using ReactiveUI;

namespace FluentFin.ViewModels;

public partial class VideoPlayerViewModel(IJellyfinClient jellyfinClient,
							TrickplayViewModel trickplayViewModel,
							ILogger<VideoPlayerViewModel> logger,
							IObservable<IInboundSocketMessage> webSocketMessages,
							INavigationService navigationService,
							ISettings settings,
							ITaskBarProgress taskBarProgress) : ObservableObject, INavigationAware
{
	private readonly CompositeDisposable _disposables = [];
	private readonly PlaybackProgressInfo _playbackProgressInfo = new();
	private KeyboardMediaPlayerController? _keyboardController;
	private PlayQueueUpdate? _playQueueUpdate;
	private TimeSpan _duration;
	private SyncPlaySendCommand? _previousCommand;


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
		_disposables.Dispose();

		NativeMethods.AllowSleep();

		taskBarProgress.Clear();
		await UpdateStatus();
		await JellyfinClient.Stop();

		if (MediaPlayer is null)
		{
			return;
		}

		MediaPlayer.Stop();
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
		if (pqu.PlayingItemIndex < 0)
		{
			return;
		}

		Playlist = PlaylistViewModel.FromSyncPlay(pqu.Playlist);
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

		MediaPlayer.Stop();

		var full = await JellyfinClient.GetItem(selectedItem.Dto.Id.Value);

		if (full is null)
		{
			return;
		}

		var mediaResponse = await GetMediaUrl(full);

		if (mediaResponse is null)
		{
			return;
		}


		await JellyfinClient.Stop();
		TrickplayViewModel.SetItem(full);

		var defaultSubtitleIndex = mediaResponse.MediaSourceInfo.DefaultSubtitleStreamIndex;
		selectedItem.Media = mediaResponse;

		var success = MediaPlayer.Play(mediaResponse.Uri, mediaResponse.MediaSourceInfo.DefaultAudioStreamIndex ?? 0);
		if (!success)
		{
			logger.LogError("Unable to open media from {URL}", mediaResponse.Uri);
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

		await JellyfinClient.Playing(selectedItem.Dto);
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

	private async Task UpdateStatus()
	{
		if (MediaPlayer is null)
		{
			return;
		}

		if (MediaPlayer.Position.Ticks == 0)
		{
			return;
		}

		if (Playlist.SelectedItem?.Media is not { } media)
		{
			return;
		}

		if (MediaPlayer.State is MediaPlayerState.Error or MediaPlayerState.Ended or MediaPlayerState.Opening)
		{
			return;
		}

		_playbackProgressInfo.ItemId = Playlist.SelectedItem.Dto.Id;
		_playbackProgressInfo.PositionTicks = MediaPlayer.Position.Ticks;
		_playbackProgressInfo.IsPaused = !MediaPlayer.IsPlaying;
		_playbackProgressInfo.MediaSourceId = Playlist.SelectedItem.Dto.Id?.ToString("N");
		_playbackProgressInfo.IsMuted = MediaPlayer.IsMuted;
		_playbackProgressInfo.PlayMethod = PlayMethod;
		_playbackProgressInfo.AudioStreamIndex = MediaPlayer.AudioTrackIndex;
		_playbackProgressInfo.SubtitleStreamIndex = MediaPlayer.SubtitleTrackIndex;
		_playbackProgressInfo.PlaybackStartTimeTicks = TimeProvider.System.GetTimestamp();
		_playbackProgressInfo.SessionId = media.PlaybackSessionId;
		_playbackProgressInfo.MediaSourceId = media.MediaSourceId;

		taskBarProgress.SetProgressPercent((int)((MediaPlayer.Position.TotalSeconds / _duration.TotalSeconds) * 100));

		await JellyfinClient.Progress(_playbackProgressInfo);
	}

	private void SubscribeEvents(IMediaPlayerController mp)
	{
		mp.Playing.Where(_ => _disposables.IsDisposed).Subscribe(_ => mp.Stop());
		mp.Ended.Where(_ => Playlist.CanSelectNext).Subscribe(_ =>
		{
			taskBarProgress.Clear();
			Playlist.SelectNext();
		});
		mp.Stopped.Subscribe(async _ => await JellyfinClient.Stop());
		mp.Errored.Subscribe(async _ =>
		{
			logger.LogError("An error occurred while playing media");
			await JellyfinClient.Stop();
		});
		mp.PositionChanged
			.Where(_ => MediaPlayer?.State == MediaPlayerState.Playing)
			.Select(x => x.Ticks)
			.Select(ticks => Segments.Any(segment => ticks > segment.StartTicks && ticks < segment.EndTicks))
			.DistinctUntilChanged()
			.ObserveOn(RxApp.MainThreadScheduler)
			.Subscribe(isVisible => IsSkipButtonVisible = isVisible);
		mp.DurationChanged.Subscribe(d => _duration = d);

		mp.MediaLoaded
		  .Where(_ => _playQueueUpdate is not null)
		  .SelectMany(_ => JellyfinClient.SignalReadyForSyncPlay(new ReadyRequestDto
		  {
			  When = TimeProvider.System.GetUtcNow(),
			  IsPlaying = mp.IsPlaying,
			  PlaylistItemId = _playQueueUpdate!.Playlist[_playQueueUpdate.PlayingItemIndex].PlaylistItemId,
			  PositionTicks = mp.Position.Ticks
		  }).ToObservable())
		  .Subscribe();
	}

	private void SubscribeWebsocketMessage()
	{
		webSocketMessages
			.ObserveOn(RxApp.MainThreadScheduler)
			.Subscribe(async message =>
			{
				if (MediaPlayer is null)
				{
					return;
				}

				switch (message)
				{
					case PlayStateMessage { Data.Command: PlaystateCommand.PlayPause }:
						await MediaPlayer.TogglePlayPlause(JellyfinClient);
						await UpdateStatus();
						break;
					case PlayStateMessage { Data.Command: PlaystateCommand.Stop }:
						MediaPlayer.Stop();
						await JellyfinClient.Stop();
						navigationService.NavigateTo<HomeViewModel>(new());
						break;
					case SyncPlayCommandMessage { Data: not null } syncPlay:

						if (syncPlay.Data.When == _previousCommand?.When)
						{
							return;
						}

						switch (syncPlay.Data.Command)
						{
							case SendCommandType.Pause:
								await SchedulePause(syncPlay.Data, MediaPlayer);
								break;
							case SendCommandType.Unpause:
								await SchedulePlay(syncPlay.Data, MediaPlayer);
								break;
							case SendCommandType.Seek:
								await SchedulePause(syncPlay.Data, MediaPlayer);
								break;
							case SendCommandType.Stop:
								MediaPlayer.Stop();
								break;
						}

						_previousCommand = syncPlay.Data;
						break;
				}
			});
	}

	private async Task SchedulePause(SyncPlaySendCommand command, IMediaPlayerController mp)
	{
		if (command is null)
		{
			return;
		}

		var currentTime = await JellyfinClient.SyncTime();
		var commandTime = command.When;

		mp.SeekTo(new TimeSpan(command.PositionTicks));
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

	private async Task SchedulePlay(SyncPlaySendCommand command, IMediaPlayerController mp)
	{
		if (command is null)
		{
			return;
		}

		var currentTime = await JellyfinClient.SyncTime();
		var commandTime = command.When;

		if (commandTime > currentTime)
		{
			mp.SeekTo(new TimeSpan(command.PositionTicks));
			await Task.Delay(commandTime - currentTime);
			mp.Play();
		}
		else
		{
			var serverPosition = command.PositionTicks + (currentTime - commandTime).Ticks;
			mp.SeekTo(new TimeSpan(serverPosition));
			mp.Play();
		}
	}
}

