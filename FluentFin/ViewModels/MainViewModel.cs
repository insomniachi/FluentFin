using CommunityToolkit.Mvvm.ComponentModel;
using DynamicData.Binding;
using FluentFin.Core.ViewModels;
using System.Reactive.Linq;

namespace FluentFin.ViewModels;

public partial class MainViewModel : ObservableObject, IMainWindowViewModel
{
	public MainViewModel(ITitleBarViewModel titleBarViewModel)
	{
		TitleBarViewModel = titleBarViewModel;

		titleBarViewModel.WhenPropertyChanged(x => x.User, notifyOnInitialValue: false)
			.Select(x => x.Value is not null)
			.Subscribe(isLoggedIin =>
			{
				ViewState = isLoggedIin ? MainWindowViewState.LoggedIn : MainWindowViewState.Login;
			});
	}
	public ITitleBarViewModel TitleBarViewModel { get; }

	[ObservableProperty]
	public partial MainWindowViewState ViewState { get; set; }
}
