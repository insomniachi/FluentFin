using CommunityToolkit.Mvvm.Input;
using FluentFin.Contracts.Services;
using FluentFin.Core.Contracts.Services;
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
}
