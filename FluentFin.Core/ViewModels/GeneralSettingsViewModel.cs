using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentFin.Contracts.ViewModels;
using FluentFin.Core.Contracts.Services;
using Jellyfin.Sdk.Generated.Models;
using ReactiveUI;

namespace FluentFin.Core.ViewModels;

public partial class GeneralSettingsViewModel(IJellyfinClient jellyfinClient) : ObservableObject, INavigationAware
{
	private ServerConfiguration? _configuration;

	[ObservableProperty]
	public partial string? ServerName { get; set; }

	[ObservableProperty]
	public partial string? CachePath { get; set; }

	[ObservableProperty]
	public partial string? MetadataPath { get; set; }

	[ObservableProperty]
	public partial bool EnableQuickConnect { get; set; }

	[ObservableProperty]
	public partial double ParallelLibraryScanTaskLimit { get; set; }

	[ObservableProperty]
	public partial double ParallelImageEncodingLimit { get; set; }

	public Task OnNavigatedFrom() => Task.CompletedTask;

	public async Task OnNavigatedTo(object parameter)
	{
		await UpdatePropertiesToViewModel();
		ListenForChanges();
	}

	private async Task UpdatePropertiesToViewModel()
	{
		_configuration = await jellyfinClient.GetConfiguration();

		if (_configuration is null)
		{
			return;
		}

		ServerName = _configuration.ServerName;
		CachePath = _configuration.CachePath;
		MetadataPath = _configuration.MetadataPath;
		EnableQuickConnect = _configuration.QuickConnectAvailable ?? false;
		ParallelLibraryScanTaskLimit = _configuration.LibraryScanFanoutConcurrency ?? 0;
		ParallelImageEncodingLimit = _configuration.ParallelImageEncodingLimit ?? 0;
	}

	private void ListenForChanges()
	{
		if(_configuration is null)
		{
			return;
		}

		this.WhenAnyValue(x => x.ServerName).Subscribe(x => _configuration.ServerName = x);
		this.WhenAnyValue(x => x.CachePath).Subscribe(x => _configuration.CachePath = x);
		this.WhenAnyValue(x => x.MetadataPath).Subscribe(x => _configuration.MetadataPath = x);
		this.WhenAnyValue(x => x.EnableQuickConnect).Subscribe(x => _configuration.QuickConnectAvailable = x);
		this.WhenAnyValue(x => x.ParallelLibraryScanTaskLimit).Subscribe(x => _configuration.LibraryScanFanoutConcurrency = (int)x);
		this.WhenAnyValue(x => x.ParallelImageEncodingLimit).Subscribe(x => _configuration.ParallelImageEncodingLimit = (int)x);
	}

	[RelayCommand]
	private async Task Save()
	{
		if (_configuration is null)
		{
			return;
		}
		await jellyfinClient.SaveConfiguration(_configuration);
	}

	[RelayCommand]
	private async Task Reset() => await UpdatePropertiesToViewModel();

}
