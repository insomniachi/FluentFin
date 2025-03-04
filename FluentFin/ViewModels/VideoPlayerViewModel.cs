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
using Microsoft.Extensions.Logging;
using ReactiveUI;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Reflection;
using System.Web;

namespace FluentFin.ViewModels;

public partial class VideoPlayerViewModel : ObservableObject, INavigationAware
{
	private readonly CompositeDisposable _disposables = new();
	private readonly IJellyfinClient _jellyfinClient;
	private readonly PlaybackProgressInfo _playbackProgressInfo = new();
	private readonly ILogger<VideoPlayerViewModel> _logger;

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
			.Where(_ => MediaPlayer.Status == Status.Playing)
			.Select(x => x.Ticks)
			.Select(ticks => Segments.Any(segment => ticks > segment.StartTicks && ticks < segment.EndTicks))
			.DistinctUntilChanged()
			.ObserveOn(RxApp.MainThreadScheduler)
			.Subscribe(isVisible => IsSkipButtonVisible = isVisible);

		MediaPlayer.WhenAnyValue(x => x.Status)
			.Do(status =>
			{
				if(status == Status.Playing && _disposables?.IsDisposed == true)
				{
					MediaPlayer.Stop();
				}
			})
			.Where(status => status is Status.Stopped or Status.Failed)
			.Subscribe(async _ => await jellyfinClient.Stop());

		MediaPlayer.WhenAnyValue(x => x.Status)
			.Where(status => status is Status.Ended)
			.Where(_ => Playlist.CanSelectNext)
			.Subscribe(_ => Playlist.SelectNext());

		webSocketMessages
			.ObserveOn(RxApp.MainThreadScheduler)
			.Subscribe(async message =>
			{
				switch (message)
				{
					case GeneralCommandMessage { Data.Name: GeneralCommandType.DisplayMessage } gcm:
						await contentDialogService.ShowMessage(gcm.Data.Arguments["Header"],
															   gcm.Data.Arguments["Text"],
															   TimeSpan.FromMilliseconds(double.Parse(gcm.Data.Arguments["TimeoutMs"])));
						break;
					case PlayStateMessage { Data.Command: Core.WebSockets.Messages.PlaystateCommand.PlayPause }:
						MediaPlayer.TogglePlayPause();
						await UpdateStatus();
						break;
					case PlayStateMessage { Data.Command: Core.WebSockets.Messages.PlaystateCommand.Stop }:
						MediaPlayer.Stop();
						await jellyfinClient.Stop();
						navigationService.NavigateTo<HomeViewModel>(new());
						break;
				}
			});
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

		var args = MediaPlayer.Open(HttpUtility.UrlDecode(mediaResponse.Uri.ToString()));
		if (!args.Success)
		{
			_logger.LogError("Unable to open media from {URL}", mediaResponse.Uri);
		}

		PlayMethod = mediaResponse.PlayMethod;

		if (selectedItem.Dto?.UserData?.PlaybackPositionTicks is { } ticks)
		{
			MediaPlayer.SeekAccurate((int)TimeSpan.FromTicks(ticks).TotalMilliseconds);
		}

		if(mediaResponse.MediaSourceInfo.MediaStreams?.FirstOrDefault(x => x.Index == defaultSubtitleIndex) is { } subtitleStream)
		{
			OpenSubtitles(mediaResponse, subtitleStream);
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
				MediaPlayer.Config.Subtitles.Enabled = true;
				var url = HttpUtility.UrlDecode(_jellyfinClient.BaseUrl.AppendPathSegment(stream.DeliveryUrl).ToString());

				MethodInfo? dynMethod = MediaPlayer.GetType().GetMethod("OpenSubtitles", BindingFlags.NonPublic | BindingFlags.Instance);
				dynMethod?.Invoke(MediaPlayer, [url]);
			}
			else
			{
				var internalSubtitleInfo = subtitles.Where(x => x.IsExternal is false or null).ToList();
				var index = internalSubtitleInfo.IndexOf(stream);
				MediaPlayer.Open(MediaPlayer.Subtitles.Streams[index]);
			}
		}
		catch { }
    }

	public TrickplayViewModel TrickplayViewModel { get; }

	public Player MediaPlayer { get; private set; } = new();

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
			.Subscribe(_ => Position = new TimeSpan(MediaPlayer.CurTime))
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

		MediaPlayer.SeekAccurate((int)TimeSpan.FromTicks(segment.EndTicks.Value).TotalMilliseconds);
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

		if (MediaPlayer.Status is Status.Failed or Status.Ended or Status.Opening)
		{
			return;
		}

		_playbackProgressInfo.ItemId = Dto.Id;
		_playbackProgressInfo.PositionTicks = Position.Ticks;
		_playbackProgressInfo.IsPaused = !MediaPlayer.IsPlaying;
		_playbackProgressInfo.MediaSourceId = Dto.Id?.ToString("N");
		_playbackProgressInfo.IsMuted = MediaPlayer.Audio.Mute;
		_playbackProgressInfo.PlayMethod = PlayMethod;
		_playbackProgressInfo.AudioStreamIndex = MediaPlayer.Audio.Streams.FirstOrDefault(x => x.Enabled)?.StreamIndex - 1;
		_playbackProgressInfo.SubtitleStreamIndex = MediaPlayer.Subtitles.Streams.FirstOrDefault(x => x.Enabled)?.StreamIndex - 1;
		_playbackProgressInfo.PlaybackStartTimeTicks = TimeProvider.System.GetTimestamp();
		_playbackProgressInfo.SessionId = media.PlaybackSessionId;
		_playbackProgressInfo.MediaSourceId = media.MediaSourceId;

		await _jellyfinClient.Progress(_playbackProgressInfo);
	}

}

