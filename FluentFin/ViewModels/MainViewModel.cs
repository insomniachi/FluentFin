using CommunityToolkit.Mvvm.ComponentModel;
using FluentFin.Core.Settings;
using FluentFin.Core.ViewModels;

namespace FluentFin.ViewModels;

public partial class MainViewModel(ITitleBarViewModel titleBarViewModel) : ObservableObject, IMainWindowViewModel
{
	public ITitleBarViewModel TitleBarViewModel { get; } = titleBarViewModel;
}
