
using CommunityToolkit.Mvvm.ComponentModel;
using FluentFin.Contracts.Services;
using FluentFin.Contracts.ViewModels;
using FluentFin.Core;
using FluentFin.Core.Services;
using FluentFin.Core.ViewModels;
using FluentFin.UI.Core.Contracts.Services;
using Microsoft.UI.Xaml.Navigation;

namespace FluentFin.ViewModels;

public partial class ShellViewModel : ObservableObject, INavigationAware
{
	[ObservableProperty] public partial bool IsBackEnabled { get; set; }
	[ObservableProperty] public partial object? Selected { get; set; }

	public INavigationService NavigationService { get; }
	public INavigationViewService NavigationViewService { get; }
	public bool IsReportingVisible { get; } = SessionInfo.HasPlaybackReporting();

	public ShellViewModel(INavigationService navigationService, INavigationViewService navigationViewService)
	{
		NavigationService = navigationService;
		NavigationService.Navigated += OnNavigated;
		NavigationViewService = navigationViewService;
	}

	private void OnNavigated(object sender, NavigationEventArgs e)
	{
		IsBackEnabled = NavigationService.CanGoBack;
		var selectedItem = NavigationViewService.GetSelectedItem(e.SourcePageType);
		Selected = selectedItem;
	}

	public Task OnNavigatedTo(object parameter)
	{
		NavigationService.NavigateTo<HomeViewModel>(parameter);
		return Task.CompletedTask;
	}

	public Task OnNavigatedFrom() => Task.CompletedTask;
}
