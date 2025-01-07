using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.Helpers;
using FluentFin.Contracts.Services;
using FluentFin.Core.Contracts.Services;
using FluentFin.Core.Services;
using FluentFin.Core.Settings;
using FluentFin.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace FluentFin.Core.ViewModels;

public partial class SelectServerViewModel(ISettings settings,
										   ILocalSettingsService localSettingsService,
										   [FromKeyedServices(NavigationRegions.InitialSetup)] INavigationService navigationService) : ObservableObject
{

	public List<SavedServer> Servers => settings.Servers;

	[RelayCommand]
	private async Task CheckConnectivityAndGoToLogin(string url)
	{
		var info = await JellyfinAuthenticationService.GetPublicInfo(url);

		if(info is null)
		{
			// show message
			return;
		}

		var server = Servers.FirstOrDefault(x => x.Id == info.Id) ?? new SavedServer()
		{
			Id = info.Id ?? "",
			DisplayName = info.ServerName ?? "",
		};

		var isLocalAddress = info.LocalAddress == url;

		if(isLocalAddress)
		{
			server.LocalUrl = url;
			server.LocalNetworkNames = [.. NetworkHelper.Instance.ConnectionInformation.NetworkNames];
		}
		else
		{
			server.PublicUrl = url;
		}

		if(!Servers.Any(x => x.Id == info.Id))
		{
			Servers.Add(server);
			localSettingsService.SaveSetting(SettingKeys.Servers, Servers);
		}

		navigationService.NavigateTo(typeof(LoginViewModel).FullName!, server);
	}
}
