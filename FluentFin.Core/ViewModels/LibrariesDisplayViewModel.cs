using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentFin.Contracts.ViewModels;
using FluentFin.Core.Contracts.Services;
using Jellyfin.Sdk.Generated.Models;

namespace FluentFin.Core.ViewModels;

public partial class LibrariesDisplayViewModel(IJellyfinClient jellyfinClient) : ObservableObject, INavigationAware
{
	private ServerConfiguration? _serverConfiguration;
	private Metadata? _metadata;

	[ObservableProperty]
	public partial string DateAddedBehaviorForNewContent { get; set; }

	[ObservableProperty]
	public partial bool DisplayFolderView { get; set; }

	[ObservableProperty]
	public partial bool DisplaySpecialsWithinSeasons { get; set; }

	[ObservableProperty]
	public partial bool GroupMoviesIntoCollections { get; set; }

	[ObservableProperty]
	public partial bool EnableExternalContentSuggestions { get; set; }

	public List<string> DateAddedBehaviors { get; set; } = ["User file creation date", "Use date scanned into the library"];

	public Task OnNavigatedFrom() => Task.CompletedTask;

	public async Task OnNavigatedTo(object parameter)
	{
		_serverConfiguration = await jellyfinClient.GetConfiguration();
		_metadata = await jellyfinClient.GetMetadata();

		if(_metadata is not null)
		{
			DateAddedBehaviorForNewContent = _metadata.UseFileCreationTimeForDateAdded ? DateAddedBehaviors[0] : DateAddedBehaviors[1];
		}

		if (_serverConfiguration is null)
		{
			return;
		}

		DisplayFolderView = _serverConfiguration.EnableFolderView ?? false;
		DisplaySpecialsWithinSeasons = _serverConfiguration.DisplaySpecialsWithinSeasons ?? false;
		GroupMoviesIntoCollections = _serverConfiguration.EnableGroupingIntoCollections ?? false;
		EnableExternalContentSuggestions = _serverConfiguration.EnableExternalContentInSuggestions ?? false;
	}

	[RelayCommand]
	private async Task Save()
	{
		await SaveConfiguration();
		await SaveMetadata();
	}

	[RelayCommand]
	private async Task Reset() => await OnNavigatedTo(new());

	private async Task SaveConfiguration()
	{
		if (_serverConfiguration is null)
		{
			return;
		}

		_serverConfiguration.EnableFolderView = DisplayFolderView;
		_serverConfiguration.DisplaySpecialsWithinSeasons = DisplaySpecialsWithinSeasons;
		_serverConfiguration.EnableGroupingIntoCollections = GroupMoviesIntoCollections;
		_serverConfiguration.EnableExternalContentInSuggestions = EnableExternalContentSuggestions;

		await jellyfinClient.SaveConfiguration(_serverConfiguration);
	}

	private async Task SaveMetadata()
	{
		if (_metadata is null)
		{
			return;
		}
		_metadata.UseFileCreationTimeForDateAdded = DateAddedBehaviorForNewContent == DateAddedBehaviors[0];
		await jellyfinClient.SaveMetadata(_metadata);
	}
}
