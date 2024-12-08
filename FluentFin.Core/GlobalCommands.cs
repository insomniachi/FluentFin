using CommunityToolkit.Mvvm.Input;
using FluentFin.Contracts.Services;
using Jellyfin.Sdk.Generated.Models;

namespace FluentFin.Core;

public partial class GlobalCommands(INavigationServiceCore navigationService)
{
	[RelayCommand]
	public void PlayDto(BaseItemDto dto)
	{
		if(dto is null)
		{
			return;
		}

		navigationService.NavigateTo("FluentFin.ViewModels.VideoPlayerViewModel", dto);
	}
}
