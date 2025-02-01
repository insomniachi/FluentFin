using FluentFin.Contracts.Services;
using FluentFin.Core;
using FluentFin.Plugins.Playback_Reporting.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;


namespace FluentFin.Plugins.Playback_Reporting.Views;

public sealed partial class PlaybackReportingDashboardPage : Page
{
	public PlaybackReportingDashboardViewModel ViewModel { get; } = Locator.GetService<PlaybackReportingDashboardViewModel>();

	public PlaybackReportingDashboardPage()
	{
		InitializeComponent();

		var navigationService = Locator.GetKeyedService<INavigationService>(NavigationRegions.PlaybackReporting);
		var navigationViewService = Locator.GetKeyedService<INavigationViewService>(NavigationRegions.PlaybackReporting);

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

	protected override void OnNavigatedTo(NavigationEventArgs e)
	{
		Locator.GetKeyedService<INavigationService>(NavigationRegions.PlaybackReporting).NavigateTo(typeof(PlaybackReportingUsersViewModel).FullName!);
	}
}
