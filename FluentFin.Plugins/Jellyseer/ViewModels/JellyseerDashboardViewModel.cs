using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentFin.Contracts.ViewModels;
using FluentFin.Core;
using FluentFin.Plugins.Jellyseer.Models;

namespace FluentFin.Plugins.Jellyseer.ViewModels;

public partial class JellyseerDashboardViewModel(IJellyseerClient jellyseerClient) : ObservableObject, INavigationAware
{
	[ObservableProperty]
	public partial bool HasServer { get; set; }

	[ObservableProperty]
	public partial ObservableCollection<MediaRequest> Requests { get; set; } = [];

	public IJellyseerClient Client { get; } = jellyseerClient;

	public Task OnNavigatedFrom() => Task.CompletedTask;

	public async Task OnNavigatedTo(object parameter)
	{
		HasServer = HasAssociatedServer();

		if (!HasServer)
		{
			return;
		}

		var loggedIn = await Client.Login();

		if (loggedIn)
		{
			var response = await Client.GetRequests();
			Requests = new(response?.Results ?? []);
		}
	}

	private static bool HasAssociatedServer()
	{
		if(JellyseerOptions.Current.ServerMapping is not { Count : > 0 })
		{
			return false;
		}
		return JellyseerOptions.Current.ServerMapping.ContainsKey(SessionInfo.ServerId);
	}

	[RelayCommand]
	private async Task SetServerUrl(string url)
	{
		JellyseerOptions.Current.ServerMapping[SessionInfo.ServerId] = url;
		var loggedIn = await Client.Login();

		if(loggedIn)
		{
			JellyseerOptions.Current.Save?.Invoke();
			HasServer = true;
			var response = await Client.GetRequests();
			Requests = new(response?.Results ?? []);
		}
	}
}
