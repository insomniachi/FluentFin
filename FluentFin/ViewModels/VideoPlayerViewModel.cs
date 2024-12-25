using CommunityToolkit.Mvvm.ComponentModel;
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
	private readonly ILogger<VideoPlayerViewModel> _logger;

	public VideoPlayerViewModel(IJellyfinClient jellyfinClient,
								ILogger<VideoPlayerViewModel> logger)
	{
		_jellyfinClient = jellyfinClient;
		_logger = logger;

		this.WhenAnyValue(x => x.Playlist.SelectedItem)
			.WhereNotNull()
			.SelectMany(item => jellyfinClient.GetMediaUrl(item.Dto))
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
	}


	public Player MediaPlayer { get; private set; } = new();
	public BaseItemDto? Dto { get; set; }

	[ObservableProperty]
	public partial TimeSpan Position { get; set; }

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
		
		Dto = dto;
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

			Playlist.Items.Add(new PlaylistItem
			{
				Title = item.Name ?? "",
				Dto = item
			});

		}

		if(Playlist.Items.Count == 0)
		{
			return false;
		}


		return true;
	}

	private Task<bool> CreateSingleItemPlaylist(BaseItemDto dto)
	{
		Playlist.Items.Add(new PlaylistItem
		{
			Title = dto.Name ?? "",
			Dto = dto
		});

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

