using System.ComponentModel;
using FluentFin.Core.Settings;
using Jellyfin.Sdk.Generated.Models;

namespace FluentFin.Core.ViewModels;

public interface IMainWindowViewModel : INotifyPropertyChanged
{
	ITitleBarViewModel TitleBarViewModel { get; }

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
	SavedServer? CurrentServer { get; set; }
	Task Logout();
}
