using FluentFin.Core.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace FluentFin.Views.JellyfinSettings;

public sealed partial class LibrariesSettingsPage : Page
{
	public LibrariesSettingsViewModel ViewModel { get; } = App.GetService<LibrariesSettingsViewModel>();

	public LibrariesSettingsPage()
	{
		InitializeComponent();
	}

	public static string SingleOrCount(IList<string> values)
	{
		return values.Count == 1 ? values[0] : $"{values.Count} folders";
	}
}
