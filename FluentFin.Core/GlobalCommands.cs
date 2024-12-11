using CommunityToolkit.Mvvm.Input;
using FluentFin.Contracts.Services;
using FluentFin.Core.Contracts.Services;
using FluentFin.Core.ViewModels;
using Jellyfin.Sdk.Generated.Models;

namespace FluentFin.Core;

public partial class GlobalCommands(INavigationServiceCore navigationService,
									IJellyfinClient jellyfinClient)
{
	[RelayCommand]
	public void PlayDto(BaseItemDto dto)
	{
		navigationService.NavigateTo("FluentFin.ViewModels.VideoPlayerViewModel", dto);
	}

	[RelayCommand]
	public async Task<UserItemDataDto?> ToggleWatched(BaseItemDto dto)
	{
		return await jellyfinClient.ToggleMarkAsWatched(dto);
	}

	[RelayCommand]
	public async Task<UserItemDataDto?> ToggleFavorite(BaseItemDto dto)
	{
		return await jellyfinClient.ToggleMarkAsFavorite(dto);
	}

	[RelayCommand]
	public void DisplayDto(BaseItemDto dto)
	{
		switch(dto.Type)
		{
			case BaseItemDto_Type.Movie:
				navigationService.NavigateTo(typeof(MovieViewModel).FullName!, dto); break;
			case BaseItemDto_Type.Series:
				navigationService.NavigateTo(typeof(SeriesViewModel).FullName!, dto); break;
			default:
				break;
		}
	}
}
