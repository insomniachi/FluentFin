using FluentFin.Core.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace FluentFin.Views.JellyfinSettings;

public sealed partial class SettingsPage : Page
{
	public SettingsViewModel ViewModel { get; } = App.GetService<SettingsViewModel>();

	public SettingsPage()
	{
		InitializeComponent();
	}
}
