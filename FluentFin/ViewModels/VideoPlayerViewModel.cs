using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentFin.Contracts.ViewModels;
using FluentFin.Core.Contracts.Services;
using FlyleafLib.MediaPlayer;
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

	public VideoPlayerViewModel(IJellyfinClient jellyfinClient,
								TrickplayViewModel trickplayViewModel,
								ILogger<VideoPlayerViewModel> logger)
	{
		_jellyfinClient = jellyfinClient;
		_logger = logger;

		TrickplayViewModel = trickplayViewModel;

		Playlist.PropertyChanged += OnPlaylistPropertyChanged;

		this.WhenAnyValue(x => x.Position)
			.Where(_ => MediaPlayer.Status == Status.Playing)
			.Select(x => x.Ticks)
			.Select(ticks => Segments.Any(segment => ticks > segment.StartTicks && ticks < segment.EndTicks))
			.DistinctUntilChanged()
			.ObserveOn(RxApp.MainThreadScheduler)
			.Subscribe(isVisible => IsSkipButtonVisible = isVisible);

		MediaPlayer.WhenAnyValue(x => x.Status)
			.Where(status => status is Status.Stopped or Status.Failed)
			.Subscribe(async _ => await jellyfinClient.Stop());
	}

	private async void OnPlaylistPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
	{
		if(e.PropertyName != nameof(Playlist.SelectedItem))
		{
			return;
		}

		if(Playlist.SelectedItem is not { } selectedItem)
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

		if(mediaResponse is null)
		{
			return;
		}


		await _jellyfinClient.Stop();
		TrickplayViewModel.SetItem(full);

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
		else
		{
			//MediaPlayer.SeekAccurate(0);
		}
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

	public PlaylistViewModel Playlist { get;  } = new PlaylistViewModel();

	public PlaybackProgressInfo_PlayMethod PlayMethod { get; private set; }

	public async Task OnNavigatedFrom()
	{
		_disposables.Dispose();

		if(Dto is null)
		{
			return;
		}

		await _jellyfinClient.Stop();

		MediaPlayer.Pause();
		MediaPlayer.Dispose();
	}

	public async Task OnNavigatedTo(object parameter)
	{
		if(parameter is not BaseItemDto dto)
		{
			return;
		}

		if(dto.Id is not { } id)
		{
			return;
		}
		
		Dto = await _jellyfinClient.GetItem(id);

		var success = dto.Type switch
		{
			BaseItemDto_Type.Movie  => await CreateSingleItemPlaylist(dto),
			BaseItemDto_Type.Episode => await CreateSeasonPlaylistFromEpisode(dto),
			BaseItemDto_Type.Series => await CreateSeriesPlaylist(dto),
			BaseItemDto_Type.Season => await CreateSeasonPlaylist(dto),
			_ => false
		};

		if(!success)
		{
			return;
		}

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

		if(segment is not { EndTicks : not null })
		{
			return;
		}

		MediaPlayer.SeekAccurate((int)TimeSpan.FromTicks(segment.EndTicks.Value).TotalMilliseconds);
	}

	private async Task<MediaResponse?> GetMediaUrl(BaseItemDto dto)
	{
		var segments = await _jellyfinClient.GetMediaSegments(dto, [MediaSegmentType.Intro, MediaSegmentType.Outro]);

		if(segments is { Items : not null})
		{
			Segments = segments.Items;
		}

		return await _jellyfinClient.GetMediaUrl(dto);
	}

	private async Task<bool> CreateSeriesPlaylist(BaseItemDto series)
	{
		var response = await _jellyfinClient.GetItems(series);

		if (response is null or { Items: null })
		{
			return false;
		}

		var season = response.Items.FirstOrDefault(x => x.UserData?.UnplayedItemCount > 0);

		if(season is null)
		{
			return false;
		}

		return await CreateSeasonPlaylist(season);
	}

	private async Task<bool> CreateSeasonPlaylistFromEpisode(BaseItemDto episode)
	{
		if(episode.SeasonId is not { } seasonId)
		{
			return false;
		}

		var season = await _jellyfinClient.GetItem(seasonId);

		if(season is null)
		{
			return false;
		}

		return await CreateSeasonPlaylist(season);
	}

	private async Task<bool> CreateSeasonPlaylist(BaseItemDto season)
	{
		var response = await _jellyfinClient.GetItems(season);

		if (response is null or { Items: null })
		{
			return false;
		}


		foreach (var item in response.Items)
		{
			if(item is null)
			{
				continue;
			}

			var playlistItem = new PlaylistItem
			{
				Title = item.Name ?? "",
				Dto = item
			};

			Playlist.Items.Add(playlistItem);
		}

		if(Playlist.Items.Count == 0)
		{
			return false;
		}


		return true;
	}

	private Task<bool> CreateSingleItemPlaylist(BaseItemDto dto)
	{
		var item = new PlaylistItem
		{
			Title = dto.Name ?? "",
			Dto = dto
		};

		Playlist.Items.Add(item);

		return Task.FromResult(true);
	}

	private async Task UpdateStatus()
	{
		if(Dto is null)
		{
			return;
		}

		if(Position.Ticks == 0)
		{
			return;
		}

		if(Playlist.SelectedItem?.Media is not { } media)
		{
			return;
		}

		if(MediaPlayer.Status is Status.Failed or Status.Ended or Status.Opening)
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

