using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Controls;
using FluentFin.Core.Settings;
using FluentFin.Core.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace FluentFin.Views.JellyfinSettings;

public sealed partial class SettingsPage : Page
{
	public SettingsViewModel ViewModel { get; } = App.GetService<SettingsViewModel>();

	public SettingsPage()
	{
		InitializeComponent();
	}

	private void RemoveUser(object sender, RoutedEventArgs e)
	{
		if (sender is not FrameworkElement button)
		{
			return;
		}

		if (button.Tag is not SavedUser user)
		{
			return;
		}

		if (button.FindAscendant<SettingsExpander>() is not { } expander)
		{
			return;
		}

		if (expander.Tag is not SavedServer server)
		{
			return;
		}

		server.Users.Remove(user);
	}
}
