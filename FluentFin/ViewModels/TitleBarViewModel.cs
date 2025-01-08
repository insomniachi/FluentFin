using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentFin.Contracts.Services;
using FluentFin.Core;
using FluentFin.Core.Contracts.Services;
using FluentFin.Core.Settings;
using FluentFin.Core.ViewModels;
using FluentFin.Views;
using Jellyfin.Sdk.Generated.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace FluentFin.ViewModels;

public partial class TitleBarViewModel : ObservableObject, ITitleBarViewModel
{
	private readonly INavigationService _navigationService;
	private readonly INavigationService _setupNavigationService;
	private readonly INavigationViewService _navigationViewService;
	private readonly IJellyfinClient _jellyfinClient;

	public TitleBarViewModel(INavigationService navigationService,
							 [FromKeyedServices(NavigationRegions.InitialSetup)]INavigationService setupNavigationService,
							 INavigationViewService navigationViewService,
							 IJellyfinClient jellyfinClient,
							 ILocalSettingsService localSettingsService)
	{
		_navigationService = navigationService;
		_setupNavigationService = setupNavigationService;
		_navigationViewService = navigationViewService;
		_jellyfinClient = jellyfinClient;

		navigationService.Navigated += NavigationService_Navigated;
	}

	[ObservableProperty]
	public partial string Title { get; set; } = "";

	[ObservableProperty]
	public partial string Version { get; set; } = Assembly.GetEntryAssembly()!.GetName().Version!.ToString();

	[ObservableProperty]
	public partial bool CanGoBack { get; set; }

	[ObservableProperty]
	public partial UserDto? User { get; set; }

	[ObservableProperty]
	public partial bool IsVisible { get; set; } = true;

	[ObservableProperty]
	public partial SavedServer? CurrentServer { get; set; }

	public void TogglePane()
	{
		_navigationViewService.TogglePane();
	}

	public void GoBack()
	{
		_navigationService.GoBack();
	}

	[RelayCommand]
	public async Task Logout()
	{
		User = null;
		_setupNavigationService.NavigateTo(typeof(SelectServerViewModel).FullName!);
		await _jellyfinClient.Logout();
	}

	[RelayCommand]
	public async Task SwitchUser()
	{
		User = null;
		_setupNavigationService.NavigateTo(typeof(LoginViewModel).FullName!, CurrentServer);
		await _jellyfinClient.Logout();
	}


	[RelayCommand]
	private void GoToDashboard()
	{
		_navigationService.NavigateTo(typeof(JellyfinSettingsViewModel).FullName!, new object());
	}

	private void NavigationService_Navigated(object sender, Microsoft.UI.Xaml.Navigation.NavigationEventArgs e)
	{
		CanGoBack = _navigationService.CanGoBack;
	}

}
