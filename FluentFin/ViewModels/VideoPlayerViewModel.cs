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

public partial class VideoPlayerViewModel : ObservableObject, INavigationAware
{
	private readonly CompositeDisposable _disposables = new();
	private readonly IJellyfinClient _jellyfinClient;
	private readonly PlaybackProgressInfo _playbackProgressInfo = new();
	private readonly ILogger<VideoPlayerViewModel> _logger;
	private readonly ISettings _settings;
	private KeyboardMediaPlayerController? _keyboardController;

	public VideoPlayerViewModel(IJellyfinClient jellyfinClient,
								TrickplayViewModel trickplayViewModel,
								ILogger<VideoPlayerViewModel> logger,
								IObservable<IInboundSocketMessage> webSocketMessages,
								IContentDialogService contentDialogService,
								INavigationService navigationService,
								ISettings settings)
	{
		_jellyfinClient = jellyfinClient;
		_logger = logger;
		_settings = settings;

		TrickplayViewModel = trickplayViewModel;

		webSocketMessages
			.ObserveOn(RxApp.MainThreadScheduler)
			.Subscribe(async message =>
			{
				if(MediaPlayer is null)
				{
					return;
				}	

				switch (message)
				{
					case GeneralCommandMessage { Data.Name: GeneralCommandType.DisplayMessage } gcm:
						await contentDialogService.ShowMessage(gcm.Data.Arguments["Header"],
															   gcm.Data.Arguments["Text"],
															   TimeSpan.FromMilliseconds(double.Parse(gcm.Data.Arguments["TimeoutMs"])));
						break;
					case PlayStateMessage { Data.Command: Core.WebSockets.Messages.PlaystateCommand.PlayPause }:
						MediaPlayer.TogglePlayPlause();
						await UpdateStatus();
						break;
					case PlayStateMessage { Data.Command: Core.WebSockets.Messages.PlaystateCommand.Stop }:
						MediaPlayer.Stop();
						await jellyfinClient.Stop();
						navigationService.NavigateTo<HomeViewModel>(new());
						break;
				}
			});

        NativeMethods.PreventSleep();
    }

	public void SubscribeEvents(IMediaPlayerController mp)
	{
        mp.Playing.Where(_ => _disposables.IsDisposed).Subscribe(_ => mp.Stop());
        mp.Ended.Where(_ => Playlist.CanSelectNext).Subscribe(_ => Playlist.SelectNext());
        mp.Stopped.Subscribe(async _ => await _jellyfinClient.Stop());
        mp.Errored.Subscribe(async _ =>
		{
			_logger.LogError("An error occurred while playing media");
			await _jellyfinClient.Stop();
        });
        mp.PositionChanged
            .Where(_ => MediaPlayer?.State == MediaPlayerState.Playing)
            .Select(x => x.Ticks)
            .Select(ticks => Segments.Any(segment => ticks > segment.StartTicks && ticks < segment.EndTicks))	
            .DistinctUntilChanged()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(isVisible => IsSkipButtonVisible = isVisible);
    }

	private async void OnPlaylistPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
	{
		if(MediaPlayer is null)
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

		var full = await _jellyfinClient.GetItem(selectedItem.Dto.Id ?? Guid.Empty);

		if (full is null)
		{
			return;
		}

		var mediaResponse = await GetMediaUrl(full);

		if (mediaResponse is null)
		{
			return;
		}


		await _jellyfinClient.Stop();
		TrickplayViewModel.SetItem(full);

        var defaultSubtitleIndex = mediaResponse.MediaSourceInfo.DefaultSubtitleStreamIndex;
        selectedItem.Media = mediaResponse;

        var success = MediaPlayer.Play(mediaResponse.Uri, mediaResponse.MediaSourceInfo.DefaultAudioStreamIndex ?? 0);
		if (!success)
		{
			_logger.LogError("Unable to open media from {URL}", mediaResponse.Uri);
		}

		PlayMethod = mediaResponse.PlayMethod;

		if (selectedItem.Dto?.UserData?.PlaybackPositionTicks is { } ticks)
		{
			MediaPlayer.SeekTo(TimeSpan.FromTicks(ticks));
		}

		if(mediaResponse.MediaSourceInfo.MediaStreams?.FirstOrDefault(x => x.Index == defaultSubtitleIndex) is { } subtitleStream)
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
				var url = HttpUtility.UrlDecode(_jellyfinClient.BaseUrl.AppendPathSegment(stream.DeliveryUrl).ToString());
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

	public TrickplayViewModel TrickplayViewModel { get; }


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

	public async Task OnNavigatedFrom()
	{
		_disposables.Dispose();

		if (Dto is null)
		{
			return;
		}

        NativeMethods.AllowSleep();

        await _jellyfinClient.Stop();

		if(MediaPlayer is null)
		{
			return;
		}

		MediaPlayer.Pause();
		MediaPlayer.Dispose();
	}

	public async Task OnNavigatedTo(object parameter)
	{
		if (parameter is not BaseItemDto dto)
		{
			return;
		}

		if (dto.Id is not { } id)
		{
			return;
		}

		MediaPlayerType = _settings.MediaPlayer;

		Dto = await _jellyfinClient.GetItem(id);

		Playlist = dto.Type switch
		{
			BaseItemDto_Type.Movie => PlaylistViewModel.FromMovie(dto),
			BaseItemDto_Type.Episode => await PlaylistViewModel.FromEpisode(_jellyfinClient, dto),
			BaseItemDto_Type.Series => await PlaylistViewModel.FromSeries(_jellyfinClient, dto),
			BaseItemDto_Type.Season => await PlaylistViewModel.FromSeason(_jellyfinClient, dto),
			_ => new PlaylistViewModel()
		};

		if (Playlist.Items.Count == 0)
		{
			return;
		}

		this.WhenAnyValue(x => x.MediaPlayer).WhereNotNull().Subscribe(SubscribeEvents);

		Playlist.PropertyChanged += OnPlaylistPropertyChanged;

		await TrickplayViewModel.Initialize();

        this.WhenAnyValue(x => x.MediaPlayer)
			.WhereNotNull()
			.Subscribe(async mp =>
			{
                _keyboardController?.UnsubscribeEvents();
				_keyboardController = new KeyboardMediaPlayerController(mp, Skip, ToggleFullScreen);

                Playlist.AutoSelect();
                await _jellyfinClient.Playing(dto);
            });

		Observable.Timer(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(20))
			.SelectMany(_ => UpdateStatus().ToObservable())
			.Subscribe()
			.DisposeWith(_disposables);
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

	private async Task<MediaResponse?> GetMediaUrl(BaseItemDto dto)
	{
		var segments = await _jellyfinClient.GetMediaSegments(dto, [MediaSegmentType.Intro, MediaSegmentType.Outro]);

		if (segments is { Items: not null })
		{
			Segments = segments.Items;
		}

		return await _jellyfinClient.GetMediaUrl(dto);
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

		await _jellyfinClient.Progress(_playbackProgressInfo);
	}

}

