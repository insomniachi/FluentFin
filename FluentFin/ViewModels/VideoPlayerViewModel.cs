using CommunityToolkit.Mvvm.ComponentModel;
using FluentFin.Contracts.ViewModels;
using FluentFin.Core.Contracts.Services;
using Jellyfin.Sdk.Generated.Models;
using Microsoft.Extensions.Logging;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.Playback;

namespace FluentFin.ViewModels;

public partial class VideoPlayerViewModel(IJellyfinClient jellyfinClient,
										  ILogger<VideoPlayerViewModel> logger) : ObservableObject, INavigationAware
{
	private readonly CompositeDisposable _disposables = new();

	public async Task OnNavigatedFrom()
	{
		_disposables.Dispose();

		if(Dto is null)
		{
			return;
		}

		await jellyfinClient.Stop(Dto);

		if(MediaPlayer is null)
		{
			return;
		}

		MediaPlayer.Source = null;
		MediaPlayer.Dispose();
		MediaPlayer = null;
	}

	public async Task OnNavigatedTo(object parameter)
	{
		if(MediaPlayer is null)
		{
			return;
		}

		if(parameter is not BaseItemDto dto)
		{
			return;
		}

		Dto = dto;

		var uri = await jellyfinClient.GetMediaUrl(dto);

		if(uri is null)
		{
			return;
		}

		try
		{
			MediaPlayer.Source = MediaSource.CreateFromUri(uri);
			MediaPlayer.Position = TimeSpan.FromTicks(dto.UserData?.PlaybackPositionTicks ?? 0);
			MediaPlayer.CurrentStateChanged += MediaPlayer_CurrentStateChanged;

			await jellyfinClient.Playing(dto);

			Observable.Timer(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(20))
				.SelectMany(_ => UpdateStatus().ToObservable())
				.Subscribe()
				.DisposeWith(_disposables);

			Observable.Timer(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1))
				.Subscribe(_ => Position = MediaPlayer.Position)
				.DisposeWith(_disposables);

		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Unhandled Exception");
		}
	}

	private async void MediaPlayer_CurrentStateChanged(MediaPlayer sender, object args)
	{
		if(Dto is null)
		{
			return;
		}

		if(sender.CurrentState is MediaPlayerState.Paused)
		{
			await jellyfinClient.Pause(Dto);
		}
	}

	public MediaPlayer? MediaPlayer { get; private set; } = new();
	public BaseItemDto? Dto { get; set; }

	[ObservableProperty]
	public partial TimeSpan Position { get; set; }


	private async Task UpdateStatus()
	{
		if(Dto is null || MediaPlayer is null)
		{
			return;
		}

		if(MediaPlayer.CurrentState is not MediaPlayerState.Playing)
		{
			return;
		}

		await jellyfinClient.Progress(Dto, MediaPlayer.Position);

	}

}

