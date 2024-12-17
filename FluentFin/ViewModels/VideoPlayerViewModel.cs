using CommunityToolkit.Mvvm.ComponentModel;
using FluentFin.Contracts.ViewModels;
using FluentFin.Core.Contracts.Services;
using FlyleafLib.MediaPlayer;
using Jellyfin.Sdk.Generated.Models;
using Microsoft.Extensions.Logging;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;

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

		MediaPlayer.Dispose();
	}

	public async Task OnNavigatedTo(object parameter)
	{

		if(parameter is not BaseItemDto dto)
		{
			return;
		}

		MediaPlayer.PropertyChanged += MediaPlayer_PropertyChanged;
		MediaPlayer.Subtitles.PropertyChanged += Subtitles_PropertyChanged;

		Dto = dto;

		var uri = await jellyfinClient.GetMediaUrl(dto);

		if(uri is null)
		{
			return;
		}

		try
		{
			var args = MediaPlayer.Open(uri.ToString());

			if(!args.Success)
			{
				logger.LogError("Unable to open media, {Error}", args.Error);
				return;
			}

			await jellyfinClient.Playing(dto);

			Observable.Timer(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(20))
				.SelectMany(_ => UpdateStatus().ToObservable())
				.Subscribe()
				.DisposeWith(_disposables);

			Observable.Timer(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1))
				.Subscribe(_ => Position = new TimeSpan(MediaPlayer.CurTime))
				.DisposeWith(_disposables);

		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Unhandled Exception");
		}
	}

	private void Subtitles_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
	{
		if(sender is not Subtitles subs)
		{
			return;
		}

		if(subs.IsOpened)
		{
			OnPropertyChanged(nameof(MediaPlayer));
		}
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
			await jellyfinClient.Pause(Dto, Position);
		}
	}


	public Player MediaPlayer { get; private set; } = new();
	public BaseItemDto? Dto { get; set; }

	[ObservableProperty]
	public partial TimeSpan Position { get; set; }


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

		await jellyfinClient.Progress(Dto, new TimeSpan(MediaPlayer.CurTime));

	}

}

