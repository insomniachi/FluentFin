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
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Web;

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


    public TrickplayViewModel TrickplayViewModel { get; } = trickplayViewModel;


    [ObservableProperty]
    public partial IMediaPlayerController? MediaPlayer { get; set; }

	[ObservableProperty]
	public partial BaseItemDto? Dto { get; set; }

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

		if (Dto is null)
		{
			return;
		}

        NativeMethods.AllowSleep();

        taskBarProgress.Clear();
        await jellyfinClient.Stop();

		if(MediaPlayer is null)
		{
			return;
		}

		MediaPlayer.Pause();
		MediaPlayer.Dispose();
	}

	public async Task OnNavigatedTo(object parameter)
	{
		MediaPlayerType = settings.MediaPlayer;

        if(parameter is BaseItemDto { Id: { } id })
        {
            await Initialize(id);
        }
        else if(parameter is PlayQueueUpdate pqu)
        {
            _playQueueUpdate = pqu;
            await InitializeForSyncPlay(pqu);
        }

        if (Dto is null || Playlist.Items.Count == 0)
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
			.Subscribe(async mp =>
			{
                _keyboardController?.UnsubscribeEvents();
				_keyboardController = new KeyboardMediaPlayerController(mp, Skip, ToggleFullScreen);

                if(_playQueueUpdate is null)
                {
                    Playlist.AutoSelect();
                }
                else
                {
                    Playlist.SelectedItem = Playlist.Items.FirstOrDefault();
                }

                await jellyfinClient.Playing(Dto);
            });

		Observable.Timer(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(20))
			.SelectMany(_ => UpdateStatus().ToObservable())
			.Subscribe()
			.DisposeWith(_disposables);
    }


    private async Task Initialize(Guid? id)
    {
        if(!id.HasValue)
        {
            return;
        }

        Dto = await jellyfinClient.GetItem(id.Value);

        if(Dto is null)
        {
            return;
        }

        Playlist = Dto.Type switch
        {
            BaseItemDto_Type.Movie => PlaylistViewModel.FromMovie(Dto),
            BaseItemDto_Type.Episode => await PlaylistViewModel.FromEpisode(jellyfinClient, Dto),
            BaseItemDto_Type.Series => await PlaylistViewModel.FromSeries(jellyfinClient, Dto),
            BaseItemDto_Type.Season => await PlaylistViewModel.FromSeason(jellyfinClient, Dto),
            _ => new PlaylistViewModel()
        };
    }

    private async Task InitializeForSyncPlay(PlayQueueUpdate pqu)
    {
        var id = pqu.Playlist[pqu.PlayingItemIndex].ItemId;
        Dto = await jellyfinClient.GetItem(id);
        Playlist = PlaylistViewModel.FromSyncPlay(pqu.Playlist);
    }

    [RelayCommand]
	private void Skip()
	{
		if(MediaPlayer is null)
		{
			return;
		}

		var currentTime = MediaPlayer.Position.Ticks;
        var segment = Segments.FirstOrDefault(x => currentTime > x.StartTicks && currentTime < x.EndTicks);

		if (segment is not { EndTicks: not null })
		{
			return;
		}

		MediaPlayer.SeekTo(TimeSpan.FromTicks(segment.EndTicks.Value));
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

        if (Playlist.SelectedItem is not { } selectedItem)
        {
            return;
        }

        MediaPlayer.Stop();

        var full = await jellyfinClient.GetItem(selectedItem.Dto.Id ?? Guid.Empty);

        if (full is null)
        {
            return;
        }

        var mediaResponse = await GetMediaUrl(full);

        if (mediaResponse is null)
        {
            return;
        }


        await jellyfinClient.Stop();
        TrickplayViewModel.SetItem(full);

        var defaultSubtitleIndex = mediaResponse.MediaSourceInfo.DefaultSubtitleStreamIndex;
        selectedItem.Media = mediaResponse;

        var success = MediaPlayer.Play(mediaResponse.Uri, mediaResponse.MediaSourceInfo.DefaultAudioStreamIndex ?? 0);
        if (!success)
        {
            logger.LogError("Unable to open media from {URL}", mediaResponse.Uri);
        }

        PlayMethod = mediaResponse.PlayMethod;

        if (selectedItem.Dto?.UserData?.PlaybackPositionTicks is { } ticks)
        {
            MediaPlayer.SeekTo(TimeSpan.FromTicks(ticks));
        }

        if (mediaResponse.MediaSourceInfo.MediaStreams?.FirstOrDefault(x => x.Index == defaultSubtitleIndex) is { } subtitleStream)
        {
            OpenSubtitles(MediaPlayer, mediaResponse, subtitleStream);
        }

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
                var url = HttpUtility.UrlDecode(jellyfinClient.BaseUrl.AppendPathSegment(stream.DeliveryUrl).ToString());
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
		var segments = await jellyfinClient.GetMediaSegments(dto, [MediaSegmentType.Intro, MediaSegmentType.Outro]);

		if (segments is { Items: not null })
		{
			Segments = segments.Items;
		}

		return await jellyfinClient.GetMediaUrl(dto);
	}

	private async Task UpdateStatus()
	{
		if(MediaPlayer is null)
		{
			return;
		}

		if (Dto is null)
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

		_playbackProgressInfo.ItemId = Dto.Id;
		_playbackProgressInfo.PositionTicks = MediaPlayer.Position.Ticks;
		_playbackProgressInfo.IsPaused = !MediaPlayer.IsPlaying;
		_playbackProgressInfo.MediaSourceId = Dto.Id?.ToString("N");
		_playbackProgressInfo.IsMuted = MediaPlayer.IsMuted;
		_playbackProgressInfo.PlayMethod = PlayMethod;
		_playbackProgressInfo.AudioStreamIndex = MediaPlayer.AudioTrackIndex;
		_playbackProgressInfo.SubtitleStreamIndex = MediaPlayer.SubtitleTrackIndex;
		_playbackProgressInfo.PlaybackStartTimeTicks = TimeProvider.System.GetTimestamp();
		_playbackProgressInfo.SessionId = media.PlaybackSessionId;
		_playbackProgressInfo.MediaSourceId = media.MediaSourceId;

        taskBarProgress.SetProgressPercent((int)((MediaPlayer.Position.TotalSeconds / _duration.TotalSeconds) * 100));

        await jellyfinClient.Progress(_playbackProgressInfo);
	}

    private void SubscribeEvents(IMediaPlayerController mp)
    {
        mp.Playing.Where(_ => _disposables.IsDisposed).Subscribe(_ => mp.Stop());
        mp.Ended.Where(_ => Playlist.CanSelectNext).Subscribe(_ =>
        {
            taskBarProgress.Clear();
            Playlist.SelectNext();
        });
        mp.Stopped.Subscribe(async _ => await jellyfinClient.Stop());
        mp.Errored.Subscribe(async _ =>
        {
            logger.LogError("An error occurred while playing media");
            await jellyfinClient.Stop();
        });
        mp.PositionChanged
            .Where(_ => MediaPlayer?.State == MediaPlayerState.Playing)
            .Select(x => x.Ticks)
            .Select(ticks => Segments.Any(segment => ticks > segment.StartTicks && ticks < segment.EndTicks))
            .DistinctUntilChanged()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(isVisible => IsSkipButtonVisible = isVisible);
        mp.DurationChanged.Subscribe(d => _duration = d);

        if(_playQueueUpdate is not null)
        {
            mp.MediaLoaded
              .SelectMany(_ => jellyfinClient.SignalReadyForSyncPlay(new ReadyRequestDto
              {
                  When = TimeProvider.System.GetUtcNow(),
                  IsPlaying = mp.IsPlaying,
                  PlaylistItemId = _playQueueUpdate.Playlist[_playQueueUpdate.PlayingItemIndex].PlaylistItemId,
                  PositionTicks = mp.Position.Ticks
              }).ToObservable())
              .Subscribe();
        }
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
                        MediaPlayer.TogglePlayPlause();
                        await UpdateStatus();
                        break;
                    case PlayStateMessage { Data.Command: PlaystateCommand.Stop }:
                        MediaPlayer.Stop();
                        await jellyfinClient.Stop();
                        navigationService.NavigateTo<HomeViewModel>(new());
                        break;
                    case SyncPlayCommandMessage { Data: not null } syncPlay:

                        switch(syncPlay.Data.Command)
                        {
                            case SendCommandType.Pause:
                                MediaPlayer.Pause();
                                break;
                            case SendCommandType.Unpause:
                                MediaPlayer.Play();
                                break;
                            case SendCommandType.Seek:
                                var ticks = syncPlay.Data.PositionTicks ?? 0;
                                MediaPlayer.SeekTo(TimeSpan.FromTicks(syncPlay.Data.PositionTicks ?? 0));

                                while(MediaPlayer.Position.Ticks < ticks)
                                {
                                    await Task.Delay(10);
                                }

                                MediaPlayer.Pause();
                                await jellyfinClient.SignalReadyForSyncPlay(new ReadyRequestDto
                                {
                                    When = TimeProvider.System.GetUtcNow(),
                                    IsPlaying = MediaPlayer.IsPlaying,
                                    PlaylistItemId = _playQueueUpdate?.Playlist[_playQueueUpdate.PlayingItemIndex].PlaylistItemId,
                                    PositionTicks = MediaPlayer.Position.Ticks
                                });
                                break;
                            case SendCommandType.Stop:
                                MediaPlayer.Stop();
                                break;
                        }

                        break;
                }
            }); 
    }
}

