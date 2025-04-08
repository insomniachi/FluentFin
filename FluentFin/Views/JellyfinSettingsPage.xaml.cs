using CommunityToolkit.Mvvm.ComponentModel;
using FluentFin.Contracts.Services;
using FluentFin.Contracts.ViewModels;
using FluentFin.Core;
using FluentFin.Core.Contracts.Services;
using FluentFin.Core.Services;
using FluentFin.Core.ViewModels;
using FluentFin.Core.WebSockets;
using FluentFin.UI.Core.Contracts.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace FluentFin.Views;

public sealed partial class JellyfinSettingsPage : Page
{
	public JellyfinSettingsViewModel ViewModel { get; } = App.GetService<JellyfinSettingsViewModel>();

	public JellyfinSettingsPage()
	{
		InitializeComponent();

		var navigationService = App.GetKeyedService<INavigationService>("Settings");
		var navigationViewService = App.GetKeyedService<INavigationViewService>("Settings");

		navigationService.Frame = NavFrame;
		navigationViewService.Initialize(NavView);
	}

	private void NavView_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args)
	{
		if (NavFrame.CanGoBack)
		{
			NavFrame.GoBack();
		}
	}
}

public partial class JellyfinSettingsViewModel : ObservableObject, INavigationAware
{
	private readonly INavigationService _navigationService;
	private readonly INavigationViewService _navigationViewService;
	private readonly IJellyfinClient _jellyfinClient;

	public JellyfinSettingsViewModel([FromKeyedServices(NavigationRegions.Settings)] INavigationService navigationService,
									 [FromKeyedServices(NavigationRegions.Settings)] INavigationViewService navigationViewService,
									 IJellyfinClient jellyfinClient)
	{
		_navigationService = navigationService;
		_navigationViewService = navigationViewService;
		_jellyfinClient = jellyfinClient;

		_navigationService.Navigated += OnNavigated;
	}

	[ObservableProperty]
	public partial NavigationViewItem? Selected { get; set; }

	private void OnNavigated(object sender, NavigationEventArgs e)
	{
		var selectedItem = _navigationViewService.GetSelectedItem(e.SourcePageType);
		if (selectedItem != null)
		{
			Selected = selectedItem;
		}
	}

	public async Task OnNavigatedFrom()
	{
		if (Selected is not { Content: "Dashboard" })
		{
			return;
		}

		await _jellyfinClient.SendWebSocketMessageWithoutData<SessionsStopMessage>();
	}

	public Task OnNavigatedTo(object parameter)
	{
		_navigationService.NavigateTo<DashboardViewModel>(new());
		return Task.CompletedTask;
	}
}
