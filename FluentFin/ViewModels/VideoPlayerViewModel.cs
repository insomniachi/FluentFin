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

	public VideoPlayerViewModel(IJellyfinClient jellyfinClient,
								TrickplayViewModel trickplayViewModel,
								ILogger<VideoPlayerViewModel> logger)
	{
		_jellyfinClient = jellyfinClient;

		TrickplayViewModel = trickplayViewModel;

		this.WhenAnyValue(x => x.Playlist.SelectedItem)
			.WhereNotNull()
			.Do(async item =>
			{
				var full = await jellyfinClient.GetItem(item.Dto.Id ?? Guid.Empty);

				if(full is null)
				{
					return;
				}

				TrickplayViewModel.SetItem(full);
			})
			.SelectMany(item => GetMediaUrl(item.Dto!))
			.WhereNotNull()
			.Subscribe(uri =>
			{
				var args = MediaPlayer.Open(HttpUtility.UrlDecode(uri.ToString()));

				if(!args.Success)
				{
					logger.LogError("Unable to open media from {URL}", uri);
				}

				if(Dto?.UserData?.PlaybackPositionTicks is { } ticks)
				{
					MediaPlayer.SeekAccurate((int)TimeSpan.FromTicks(ticks).TotalMilliseconds);
				}
			});

		this.WhenAnyValue(x => x.Position)
			.Where(_ => MediaPlayer.Status == Status.Playing)
			.Select(x => x.Ticks)
			.Select(ticks => Segments.Any(segment => ticks > segment.StartTicks && ticks < segment.EndTicks))
			.DistinctUntilChanged()
			.ObserveOn(RxApp.MainThreadScheduler)
			.SubscribeOn(RxApp.MainThreadScheduler)
			.Subscribe(isVisible => IsSkipButtonVisible = isVisible);
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

	public PlaylistViewModel Playlist { get; } = new PlaylistViewModel();

	public async Task OnNavigatedFrom()
	{
		_disposables.Dispose();

		if(Dto is null)
		{
			return;
		}

		await _jellyfinClient.Stop(Dto);

		if(MediaPlayer is null)
		{
			return;
		}

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
		MediaPlayer.PropertyChanged += MediaPlayer_PropertyChanged;


		var success = dto.Type switch
		{
			BaseItemDto_Type.Movie or BaseItemDto_Type.Episode => await CreateSingleItemPlaylist(dto),
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

	private async Task<Uri?> GetMediaUrl(BaseItemDto dto)
	{
		var segments = await _jellyfinClient.GetMediaSegments(dto, [MediaSegmentType.Intro, MediaSegmentType.Outro]);

		if(segments is { Items : not null})
		{
			Segments = segments.Items;
		}

		return await _jellyfinClient.GetMediaUrl(dto);
	}

	private async Task<bool> CreateSeriesPlaylist(BaseItemDto dto)
	{
		var response = await _jellyfinClient.GetItems(dto);

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

	private async Task<bool> CreateSeasonPlaylist(BaseItemDto dto)
	{
		var response = await _jellyfinClient.GetItems(dto);

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

	private async void MediaPlayer_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
	{
		if(sender is not Player mp)
		{
			return;
		}

		if (Dto is null)
		{
			return;
		}

		if (e.PropertyName != nameof(MediaPlayer.Status))
		{
			return;
		}

		if(mp.Status == Status.Paused)
		{
			await _jellyfinClient.Pause(Dto, Position);
		}
	}

	private async Task UpdateStatus()
	{
		if(Dto is null)
		{
			return;
		}

		if(MediaPlayer.Status is not Status.Playing)
		{
			return;
		}

		await _jellyfinClient.Progress(Dto, new TimeSpan(MediaPlayer.CurTime));

	}

}

