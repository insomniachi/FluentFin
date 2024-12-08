using CommunityToolkit.Mvvm.Input;
using FluentFin.Contracts.Services;
using Jellyfin.Sdk.Generated.Models;

namespace FluentFin.Core;

public partial class GlobalCommands(INavigationServiceCore navigationService)
{
	[RelayCommand]
	public async Task PlayDto(BaseItemDto dto)
	{
		if(dto is null)
		{
			return;
		}

		await Task.Delay(1);

		navigationService.NavigateTo("FluentFin.ViewModels.VideoPlayerViewModel", dto);
	}
}
