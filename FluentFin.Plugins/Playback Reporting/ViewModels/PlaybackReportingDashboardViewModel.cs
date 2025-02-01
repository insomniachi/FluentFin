using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentFin.Contracts.Services;
using FluentFin.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace FluentFin.Plugins.Playback_Reporting.ViewModels;

public partial class PlaybackReportingDashboardViewModel([FromKeyedServices(NavigationRegions.PlaybackReporting)] INavigationService navigationService,
														 [FromKeyedServices(NavigationRegions.PlaybackReporting)] INavigationViewService navigationViewService) : ObservableObject
{

	[ObservableProperty]
	public partial NavigationViewItem? Selected { get; set; }

	private void OnNavigated(object sender, NavigationEventArgs e)
	{
		var selectedItem = navigationViewService.GetSelectedItem(e.SourcePageType);
		if (selectedItem != null)
		{
			Selected = selectedItem;
		}
	}

	public Task OnNavigatedFrom() => Task.CompletedTask;

	public Task OnNavigatedTo(object parameter)
	{
		navigationService.Navigated += OnNavigated;
		//navigationService.NavigateTo(typeof(DashboardViewModel).FullName!, new());
		return Task.CompletedTask;
	}
}
