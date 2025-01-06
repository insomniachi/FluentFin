using Jellyfin.Sdk.Generated.Models;
using System.ComponentModel;

namespace FluentFin.Core.ViewModels;

public enum MainWindowViewState
{
	Login,
	LoggedIn,
	LoginFailed
}

public interface IMainWindowViewModel : INotifyPropertyChanged
{
	ITitleBarViewModel TitleBarViewModel { get; }
	MainWindowViewState ViewState { get; set; }
}

public interface ITitleBarViewModel : INotifyPropertyChanged
{
	string Title { get; }
	string Version { get; }
	void TogglePane();
	void GoBack();
	bool CanGoBack { get; }
	UserDto? User { get; set; }
	bool IsVisible { get; set; }
	Task Logout();
}
