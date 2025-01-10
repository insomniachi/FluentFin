using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.Helpers;
using DynamicData;
using FluentFin.Contracts.Services;
using FluentFin.Core.Settings;
using FluentFin.Helpers;
using FluentFin.Services;
using FluentFin.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Reactive.Concurrency;

namespace FluentFin.Core.ViewModels;

public partial class SelectServerViewModel(ISettings settings,
										   [FromKeyedServices(NavigationRegions.InitialSetup)] INavigationService navigationService) : ObservableObject
{
	private readonly HttpClient _httpClient = new();


	[ObservableProperty]
	public partial bool HasServers { get; set; } = settings.Servers.Count > 0;

	[ObservableProperty]
	public partial bool IsDetecting { get; set; }

	public ObservableCollection<SavedServer> Servers => settings.Servers;


	[RelayCommand]
	private void DetectServers()
	{
		IsDetecting = true;
		Task.Run(() => NetworkDiscoveryHelper.PingAll(async (device) => await DeviceDiscovered(device), () => RxApp.MainThreadScheduler.Schedule(() => IsDetecting = false)));
	}

	[RelayCommand]
	private async Task CheckConnectivityAndGoToLogin(string url)
	{
		var server = await TryCreateSavedServer(url);

		if(server is null)
		{
			return;
		}

		navigationService.NavigateTo(typeof(LoginViewModel).FullName!, server);
	}

	private async Task DeviceDiscovered(string device)
	{
		string[] addresses = [$"https://{device}:8920", $"http://{device}:8096"];

		foreach (var address in addresses)
		{
			HttpResponseMessage response;
			
			try
			{
				response = await _httpClient.GetAsync(address);
			}
			catch
			{
				continue;
			}

			var uri = response.RequestMessage?.RequestUri?.AbsoluteUri;

			if (uri is null)
			{
				return;
			}

			var builder = new UriBuilder(uri);
			var segments = builder.Uri.Segments.ToList();
			segments.Remove(["/", "web/"]);
			var baseUrl = $"{builder.Scheme}://{builder.Host}:{builder.Port}/{string.Join("/", segments)}";
			var server = await TryCreateSavedServer(baseUrl);

			if (server is not null)
			{
				break;
			}
		}
	}

	private async Task<SavedServer?> TryCreateSavedServer(string url)
	{
		var info = await JellyfinAuthenticationService.GetPublicInfo(url);

		if (info is null)
		{
			// show message
			return null;
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

		return server;
	}

}
