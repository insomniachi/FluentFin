using CommunityToolkit.Mvvm.ComponentModel;
using FluentFin.Contracts.Services;
using FluentFin.Core;
using FluentFin.Core.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace FluentFin.ViewModels;

public partial class MainViewModel : ObservableObject, IMainWindowViewModel
{
	public MainViewModel(ITitleBarViewModel titleBarViewModel,
						 [FromKeyedServices(NavigationRegions.InitialSetup)] INavigationService navigationService)
	{
		TitleBarViewModel = titleBarViewModel;
	}
	public ITitleBarViewModel TitleBarViewModel { get; }

	[ObservableProperty]
	public partial MainWindowViewState ViewState { get; set; } = MainWindowViewState.SelectServer;

}
