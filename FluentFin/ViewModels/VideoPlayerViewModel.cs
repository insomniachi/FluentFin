using CommunityToolkit.Mvvm.ComponentModel;
using FluentFin.Contracts.ViewModels;
using FluentFin.Core.Contracts.Services;
using Jellyfin.Sdk.Generated.Models;
using Microsoft.Extensions.Logging;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage.Streams;

namespace FluentFin.ViewModels;

public partial class VideoPlayerViewModel(IJellyfinClient jellyfinClient,
										  ILogger<VideoPlayerViewModel> logger) : ObservableObject, INavigationAware
{
	public Task OnNavigatedFrom() => Task.CompletedTask;

	public async Task OnNavigatedTo(object parameter)
	{
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
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Unhandled Exception");
		}
	}

	public MediaPlayer MediaPlayer { get; } = new();
	public BaseItemDto? Dto { get; set; }
}

