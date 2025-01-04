using CommunityToolkit.Mvvm.ComponentModel;
using FluentFin.Contracts.ViewModels;
using FluentFin.Core.Contracts.Services;
using Flurl;
using Jellyfin.Sdk.Generated.Models;

namespace FluentFin.Core.ViewModels;

public partial class LibrariesSettingsViewModel(IJellyfinClient jellyfinClient) : ObservableObject, INavigationAware
{

	[ObservableProperty]
	public partial List<VirtualFolderInfo> VirtualFolders { get; set; } = [];


	public Task OnNavigatedFrom() => Task.CompletedTask;

	public async Task OnNavigatedTo(object parameter)
	{
		VirtualFolders = await jellyfinClient.GetVirtualFolders();
	}
}
