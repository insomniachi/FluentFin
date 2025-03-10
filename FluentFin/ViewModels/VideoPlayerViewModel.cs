using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentFin.Contracts.Services;
using FluentFin.Contracts.ViewModels;
using FluentFin.Core.Contracts.Services;
using FluentFin.Core.Services;
using FluentFin.Core.ViewModels;
using FluentFin.Core.WebSockets;
using FluentFin.Services;
using Flurl;
using FlyleafLib.MediaPlayer;
using Jellyfin.Sdk.Generated.Models;
using LibVLCSharp.Shared;
using Microsoft.Extensions.Logging;
using ReactiveUI;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Reflection;
using System.Web;
using ReactiveMarbles.ObservableEvents;

namespace FluentFin.ViewModels;

public partial class VideoPlayerViewModel : ObservableObject, INavigationAware
{
	private readonly CompositeDisposable _disposables = new();
	private readonly IJellyfinClient _jellyfinClient;
	private readonly PlaybackProgressInfo _playbackProgressInfo = new();
	private readonly ILogger<VideoPlayerViewModel> _logger;
	private LibVLC _libVlc;

	public VideoPlayerViewModel(IJellyfinClient jellyfinClient,
								TrickplayViewModel trickplayViewModel,
								ILogger<VideoPlayerViewModel> logger,
								IObservable<IInboundSocketMessage> webSocketMessages,
								IContentDialogService contentDialogService,
								INavigationService navigationService)
	{
		_jellyfinClient = jellyfinClient;
		_logger = logger;

		TrickplayViewModel = trickplayViewModel;

		this.WhenAnyValue(x => x.Position)
			.Where(_ => MediaPlayer?.State == VLCState.Playing)
			.Select(x => x.Ticks)
			.Select(ticks => Segments.Any(segment => ticks > segment.StartTicks && ticks < segment.EndTicks))
			.DistinctUntilChanged()
			.ObserveOn(RxApp.MainThreadScheduler)
			.Subscribe(isVisible => IsSkipButtonVisible = isVisible);

		//MediaPlayer.WhenAnyValue(x => x.Status)
		//	.Do(status =>
		//	{
		//		if(status == Status.Playing && _disposables?.IsDisposed == true)
		//		{
		//			MediaPlayer.Stop();
		//		}
		//	})
		//	.Where(status => status is Status.Stopped or Status.Failed)
		//	.Subscribe(async _ => await jellyfinClient.Stop());

		//MediaPlayer.WhenAnyValue(x => x.Status)
		//	.Where(status => status is Status.Ended)
		//	.Where(_ => Playlist.CanSelectNext)
		//	.Subscribe(_ => Playlist.SelectNext());

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
						if(MediaPlayer.State == VLCState.Playing)
						{
							MediaPlayer.Pause();
						}
						else if(MediaPlayer.State == VLCState.Stopped)
						{
							MediaPlayer.Play();
						}
						await UpdateStatus();
						break;
					case PlayStateMessage { Data.Command: Core.WebSockets.Messages.PlaystateCommand.Stop }:
						MediaPlayer.Stop();
						await jellyfinClient.Stop();
						navigationService.NavigateTo<HomeViewModel>(new());
						break;
				}
			});

		//MediaPlayer.Config.Player.KeyBindings.Remove(KeyBindingAction.ToggleSeekAccurate);
		//MediaPlayer.Config.Player.KeyBindings.AddCustom(System.Windows.Input.Key.S, true, () => Skip(MediaPlayer.CurTime), "Skip media section");
	}

	public void SetMediaPlayer(string[] options)
	{
        _libVlc = new LibVLC(enableDebugLogs: true, options);
        MediaPlayer = new MediaPlayer(_libVlc);
    }

	private async void OnPlaylistPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
	{
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

		var url = HttpUtility.UrlDecode(mediaResponse.Uri.ToString());
		var media = new Media(_libVlc, mediaResponse.Uri);
        var success = MediaPlayer.Play(media);
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
			//OpenSubtitles(mediaResponse, subtitleStream);
        }

    }

	private void OpenSubtitles(MediaResponse response, MediaStream stream)
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
				//MediaPlayer.Config.Subtitles.Enabled = true;
				var url = HttpUtility.UrlDecode(_jellyfinClient.BaseUrl.AppendPathSegment(stream.DeliveryUrl).ToString());

				MethodInfo? dynMethod = MediaPlayer.GetType().GetMethod("OpenSubtitles", BindingFlags.NonPublic | BindingFlags.Instance);
				dynMethod?.Invoke(MediaPlayer, [url]);
			}
			else
			{
				var internalSubtitleInfo = subtitles.Where(x => x.IsExternal is false or null).ToList();
				var index = internalSubtitleInfo.IndexOf(stream);
				//MediaPlayer.Open(MediaPlayer.Subtitles.Streams[index]);
			}
		}
		catch { }
    }

	public TrickplayViewModel TrickplayViewModel { get; }


	[ObservableProperty]
    public partial MediaPlayer MediaPlayer { get; set; }

	[ObservableProperty]
	public partial BaseItemDto? Dto { get; set; }

	[ObservableProperty]
	public partial TimeSpan Position { get; set; }

	[ObservableProperty]
	public partial List<MediaSegmentDto> Segments { get; set; } = [];

	[ObservableProperty]
	public partial bool IsSkipButtonVisible { get; set; }

	[ObservableProperty]
	public partial PlaylistViewModel Playlist { get; set; } = new PlaylistViewModel();

	public PlaybackProgressInfo_PlayMethod PlayMethod { get; private set; }

	public async Task OnNavigatedFrom()
	{
		_disposables.Dispose();

		if (Dto is null)
		{
			return;
		}

		await _jellyfinClient.Stop();

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

		Playlist.PropertyChanged += OnPlaylistPropertyChanged;

		await TrickplayViewModel.Initialize();

		Playlist.AutoSelect();

		await _jellyfinClient.Playing(dto);

		Observable.Timer(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(20))
			.SelectMany(_ => UpdateStatus().ToObservable())
			.Subscribe()
			.DisposeWith(_disposables);

		Observable.Timer(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1))
			.Where(_ => MediaPlayer.Time > 0)
			.Subscribe(_ => Position = TimeSpan.FromMilliseconds(MediaPlayer.Time))
			.DisposeWith(_disposables);
    }

	[RelayCommand]
	private void Skip(long currentTime)
	{
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
		if (Dto is null)
		{
			return;
		}

		if (Position.Ticks == 0)
		{
			return;
		}

		if (Playlist.SelectedItem?.Media is not { } media)
		{
			return;
		}

		if (MediaPlayer.State is VLCState.Error or VLCState.Ended or VLCState.Opening)
		{
			return;
		}

		_playbackProgressInfo.ItemId = Dto.Id;
		_playbackProgressInfo.PositionTicks = Position.Ticks;
		_playbackProgressInfo.IsPaused = !MediaPlayer.IsPlaying;
		_playbackProgressInfo.MediaSourceId = Dto.Id?.ToString("N");
		_playbackProgressInfo.IsMuted = MediaPlayer.Mute;
		_playbackProgressInfo.PlayMethod = PlayMethod;
		//_playbackProgressInfo.AudioStreamIndex = MediaPlayer.Audio.Streams.FirstOrDefault(x => x.Enabled)?.StreamIndex - 1;
		//_playbackProgressInfo.SubtitleStreamIndex = MediaPlayer.Subtitles.Streams.FirstOrDefault(x => x.Enabled)?.StreamIndex - 1;
		_playbackProgressInfo.PlaybackStartTimeTicks = TimeProvider.System.GetTimestamp();
		_playbackProgressInfo.SessionId = media.PlaybackSessionId;
		_playbackProgressInfo.MediaSourceId = media.MediaSourceId;

		await _jellyfinClient.Progress(_playbackProgressInfo);
	}

}

