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
using FluentFin.Core;
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
							ISettings settings,
							ITaskBarProgress taskBarProgress) : ObservableObject, INavigationAware
{
	private readonly CompositeDisposable _disposables = [];
	private readonly PlaybackProgressInfo _playbackProgressInfo = new();
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
		_disposables.Dispose();
		_keyboardController?.UnsubscribeEvents();

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
		else if (parameter is PlaybackRequest { Dto.Id: { } requestedId } request)
		{
			await Initialize(requestedId);

			if (Playlist.Items.FirstOrDefault(x => x.Dto.Id == requestedId) is { } requestedItem)
			{
				requestedItem.Selection = request.Selection;
			}
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

		MediaPlayer.Stop();

		var full = await JellyfinClient.GetItem(selectedItem.Dto.Id.Value);

		if (full is null)
		{
			return;
		}

		var mediaResponse = await GetMediaUrl(full, selectedItem.Selection);

		if (mediaResponse is null)
		{
			return;
		}


		await JellyfinClient.Stop();
		TrickplayViewModel.SetItem(full);

		var selection = selectedItem.Selection;
		var subtitleIndex = selection?.SubtitleStreamIndex ?? mediaResponse.MediaSourceInfo.DefaultSubtitleStreamIndex;
		var audioIndex = selection?.AudioStreamIndex ?? mediaResponse.MediaSourceInfo.DefaultAudioStreamIndex;
		selectedItem.Media = mediaResponse;

		// A transcode already carries the chosen audio stream; for direct play the player has to switch tracks itself.
		var audioOrdinal = selection?.AudioStreamIndex is { } selectedAudio && mediaResponse.PlayMethod == PlaybackProgressInfo_PlayMethod.DirectPlay
			? mediaResponse.MediaSourceInfo.MediaStreams?.Where(x => x.Type == MediaStream_Type.Audio).ToList().FindIndex(x => x.Index == selectedAudio)
			: null;

		LogMediaInfo(selectedItem.Dto, mediaResponse, selection, audioIndex, subtitleIndex);

		var success = MediaPlayer.Play(mediaResponse.Uri, audioIndex ?? 0, audioOrdinal is >= 0 ? audioOrdinal : null);
		if (!success)
		{
			logger.LogError("Unable to open media from {URL}", SubtitleStreamDiagnostics.WithoutToken(mediaResponse.Uri.ToString()));
		}

		PlayMethod = mediaResponse.PlayMethod;

		if (selectedItem.Dto.UserData?.PlaybackPositionTicks is { } ticks)
		{
			MediaPlayer.SeekTo(TimeSpan.FromTicks(ticks));
		}

		if (mediaResponse.MediaSourceInfo.MediaStreams?.FirstOrDefault(x => x.Type == MediaStream_Type.Subtitle && x.Index == subtitleIndex) is { } subtitleStream)
		{
			_ = OpenSubtitlesWhenReady(MediaPlayer, selectedItem, mediaResponse, subtitleStream);
		}
		else
		{
			logger.LogInformation("No subtitle opened at start: wanted stream index {Index} (selection={Selection})", subtitleIndex, selection?.SubtitleStreamIndex);
		}

		await JellyfinClient.Playing(selectedItem.Dto);
	}

	/// <summary>
	/// A subtitle that is a track of the player can only be selected once the player knows its tracks, which is some time after
	/// opening the media for the backends that load them in the background.
	/// </summary>
	private async Task OpenSubtitlesWhenReady(IMediaPlayerController mp, PlaylistItem item, MediaResponse response, MediaStream stream)
	{
		if (!stream.UsesDeliveryUrl(response.PlayMethod))
		{
			var subtitles = response.MediaSourceInfo.MediaStreams?.Where(x => x.Type == MediaStream_Type.Subtitle).ToList() ?? [];
			var ordinal = subtitles.Where(x => x.IsExternal is false).IndexOf(stream);

			for (var attempt = 0; attempt < 40 && mp.SubtitleTrackCount <= ordinal; attempt++)
			{
				await Task.Delay(TimeSpan.FromMilliseconds(250));

				// another item or another stream of the same item has been started in the meantime
				if (!ReferenceEquals(item.Media, response))
				{
					return;
				}
			}

			if (mp.SubtitleTrackCount <= ordinal)
			{
				logger.LogWarning("The player reported {Count} subtitle tracks after 10 seconds, trying to open track {Ordinal} anyway", mp.SubtitleTrackCount, ordinal);
			}
		}

		OpenSubtitles(mp, response, stream);
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
			if (stream.UsesDeliveryUrl(response!.PlayMethod))
			{
				var url = HttpUtility.UrlDecode(JellyfinClient.BaseUrl.AppendPathSegment(stream.DeliveryUrl).ToString());
				logger.LogInformation("Opening subtitle {Stream} as external file with {Player}, playMethod={PlayMethod}, url={Url}",
					stream.Describe(), mp.GetType().Name, response.PlayMethod, SubtitleStreamDiagnostics.WithoutToken(url));
				mp.OpenExternalSubtitleTrack(url);
			}
			else
			{
				var subtitleIndex = subtitles.Where(x => x.IsExternal is false).IndexOf(stream);
				if (stream.Index is not { } trackIndex)
				{
					logger.LogWarning("Not opening subtitle {Stream}: it has no stream index", stream.Describe());
					return;
				}

				logger.LogInformation("Opening subtitle {Stream} as internal track with {Player}, playMethod={PlayMethod}, trackIndex={TrackIndex}, ordinalAmongEmbedded={Ordinal}",
					stream.Describe(), mp.GetType().Name, response.PlayMethod, trackIndex, subtitleIndex);
				mp.OpenInternalSubtitleTrack(trackIndex, subtitleIndex);
			}
		}
		catch (Exception ex)
		{
			logger.LogWarning(ex, "Unable to open subtitle {Stream} with {Player}", stream.Describe(), mp.GetType().Name);
		}
	}

	private void LogMediaInfo(BaseItemDto item, MediaResponse response, PlaybackSelection? selection, int? audioIndex, int? subtitleIndex)
	{
		var source = response.MediaSourceInfo;

		logger.LogInformation(
			"Playing {Name} ({Id}): method={PlayMethod}, container={Container}, bitrate={Bitrate}, size={Size}, directPlay={DirectPlay}, directStream={DirectStream}, transcoding={Transcoding}",
			item.Name, item.Id, response.PlayMethod, source.Container, source.Bitrate, source.Size, source.SupportsDirectPlay, source.SupportsDirectStream, source.SupportsTranscoding);

		logger.LogInformation(
			"Stream selection: page selection (audio={SelectedAudio}, subtitle={SelectedSubtitle}), server defaults (audio={DefaultAudio}, subtitle={DefaultSubtitle}), used (audio={UsedAudio}, subtitle={UsedSubtitle})",
			selection?.AudioStreamIndex, selection?.SubtitleStreamIndex, source.DefaultAudioStreamIndex, source.DefaultSubtitleStreamIndex, audioIndex, subtitleIndex);

		logger.LogInformation("Stream URL: {Url}", SubtitleStreamDiagnostics.WithoutToken(response.Uri.ToString()));

		if (response.PlayMethod == PlaybackProgressInfo_PlayMethod.Transcode)
		{
			logger.LogInformation("Transcode: container={Container}, protocol={Protocol}", source.TranscodingContainer, source.TranscodingSubProtocol);
		}

		foreach (var stream in source.MediaStreams ?? [])
		{
			logger.LogInformation(
				"Source stream #{Index} {Type}: codec={Codec}, language={Language}, title={Title}, {Width}x{Height}, channels={Channels}, bitrate={Bitrate}, default={Default}, external={External}",
				stream.Index, stream.Type, stream.Codec, stream.Language, stream.DisplayTitle, stream.Width, stream.Height, stream.Channels, stream.BitRate, stream.IsDefault, stream.IsExternal);

			if (stream.Type == MediaStream_Type.Subtitle)
			{
				logger.LogInformation("Subtitle delivery of {Stream}", stream.Describe());
			}
		}
	}


	private async Task<MediaResponse?> GetMediaUrl(BaseItemDto dto, PlaybackSelection? selection)
	{
		var segments = await JellyfinClient.GetMediaSegments(dto, [MediaSegmentType.Intro, MediaSegmentType.Outro]);

		if (segments is { Items: not null })
		{
			Segments = segments.Items;
		}

		return await JellyfinClient.GetMediaUrl(dto, selection);
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
								MediaPlayer.Stop();
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

