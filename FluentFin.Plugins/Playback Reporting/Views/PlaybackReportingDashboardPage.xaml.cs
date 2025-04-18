using FluentFin.Core;
using FluentFin.Core.Services;
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


		ViewModel.NavigationService.Frame = NavFrame;
		ViewModel.NavigationViewService.Initialize(NavView);
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
		ViewModel.NavigationService.NavigateTo<UsersReportViewModel>();
	}
}
