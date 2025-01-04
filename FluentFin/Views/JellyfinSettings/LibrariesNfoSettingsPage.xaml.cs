using FluentFin.Core.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace FluentFin.Views.JellyfinSettings;

public sealed partial class LibrariesNfoSettingsPage : Page
{
	public LibrariesNfoSettingsViewModel ViewModel { get; } = App.GetService<LibrariesNfoSettingsViewModel>();

	public LibrariesNfoSettingsPage()
	{
		InitializeComponent();
	}
}
