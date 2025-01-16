using CommunityToolkit.Mvvm.ComponentModel;
using DynamicData;
using FluentFin.Contracts.ViewModels;
using FluentFin.Core.Contracts.Services;
using Jellyfin.Sdk.Generated.Models;
using System.Collections.ObjectModel;

namespace FluentFin.Core.ViewModels;

public partial class LibrariesSettingsViewModel(IJellyfinClient jellyfinClient) : ObservableObject, INavigationAware
{

	public ObservableCollection<VirtualFolderInfo> VirtualFolders { get; } = [];


	public Task OnNavigatedFrom() => Task.CompletedTask;

	public async Task OnNavigatedTo(object parameter)
	{
		VirtualFolders.Clear();
		VirtualFolders.AddRange(await jellyfinClient.GetVirtualFolders());
	}
}
