using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentFin.Contracts.ViewModels;
using FluentFin.Core.Contracts.Services;
using FluentFin.Core.Services;
using Jellyfin.Sdk.Generated.Models;

namespace FluentFin.Core.ViewModels;

public partial class LibrariesLandingPageViewModel(IJellyfinClient jellyfinClient,
												   INavigationServiceCore navigationService) : ObservableObject, INavigationAware
{
	public ObservableCollection<BaseItemDto> Libraries { get; } = [];

	public Task OnNavigatedFrom() => Task.CompletedTask;

	public async Task OnNavigatedTo(object parameter)
	{
		await foreach (var item in jellyfinClient.GetUserLibraries())
		{
			Libraries.Add(item);
		}
	}

	public void NavigateToLibrary(BaseItemDto library)
	{
		navigationService.NavigateTo<LibraryViewModel>(library);
	}
}
