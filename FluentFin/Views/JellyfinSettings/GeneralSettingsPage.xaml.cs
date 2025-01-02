using FluentFin.Core.ViewModels;
using Microsoft.UI.Xaml.Controls;


namespace FluentFin.Views.JellyfinSettings;

public sealed partial class GeneralSettingsPage : Page
{
	public GeneralSettingsViewModel ViewModel { get; } = App.GetService<GeneralSettingsViewModel>();

	public GeneralSettingsPage()
	{
		InitializeComponent();
	}
}
