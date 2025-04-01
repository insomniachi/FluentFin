using System.Collections.ObjectModel;
using System.Reactive.Concurrency;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.Helpers;
using FluentFin.Contracts.Services;
using FluentFin.Core.Services;
using FluentFin.Core.Settings;
using FluentFin.Helpers;
using FluentFin.Services;
using FluentFin.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;

namespace FluentFin.Core.ViewModels;

public partial class SelectServerViewModel(ISettings settings,
										   [FromKeyedServices(NavigationRegions.InitialSetup)] INavigationService navigationService,
										   IContentDialogService contentDialogService) : ObservableObject
{

	[ObservableProperty]
	public partial bool HasServers { get; set; } = settings.Servers.Count > 0;

	[ObservableProperty]
	public partial bool IsDetecting { get; set; }

	public ObservableCollection<SavedServer> Servers => settings.Servers;


	[RelayCommand]
	private async Task DetectServers()
	{
		IsDetecting = true;
		await ServerDiscovery.DiscoverServersAsync(ServerDiscovered, () => RxApp.MainThreadScheduler.Schedule(() => IsDetecting = false));
	}

	[RelayCommand]
	private async Task CheckConnectivityAndGoToLogin(string url)
	{
		var info = await JellyfinAuthenticationService.GetPublicInfo(url);

		if (info is null)
		{
			await contentDialogService.ShowMessage("Server not found", $"Unable to find a jellyfin server at {url}");
			return;
		}

		var server = Servers.FirstOrDefault(x => x.Id == info.Id) ?? new SavedServer()
		{
			Id = info.Id ?? "",
			DisplayName = info.ServerName ?? "",
			LocalUrl = info.LocalAddress ?? ""
		};

		var networkNames = NetworkHelper.Instance.ConnectionInformation.NetworkNames;
		var isLocalAddress = url.IsLocalUrl() || server.LocalNetworkNames.SequenceEqual(networkNames);

		if (isLocalAddress)
		{
			if (server.LocalNetworkNames.Count == 0)
			{
				// bug in version 10.10.5
				if (info.Version == @"10.10.5")
				{
					server.LocalUrl = url;
				}
				server.LocalNetworkNames = [.. networkNames];
				settings.SaveServerDetails();
			}
		}
		else
		{
			server.PublicUrl = url;
		}

		if (!Servers.Any(x => x.Id == info.Id))
		{
			RxApp.MainThreadScheduler.Schedule(() =>
			{
				Servers.Add(server);
				HasServers = true;
			});
		}

		navigationService.NavigateTo<LoginViewModel>(server);
	}

	private void ServerDiscovered(DiscoveryInfo info)
	{
		var server = Servers.FirstOrDefault(x => x.Id == info.Id) ?? new SavedServer
		{
			Id = info.Id,
			DisplayName = info.Name,
			LocalUrl = info.Address,
			LocalNetworkNames = [.. NetworkHelper.Instance.ConnectionInformation.NetworkNames]
		};

		if (!Servers.Any(x => x.Id == info.Id))
		{
			RxApp.MainThreadScheduler.Schedule(() =>
			{
				Servers.Add(server);
				HasServers = true;
			});
		}
	}
}
