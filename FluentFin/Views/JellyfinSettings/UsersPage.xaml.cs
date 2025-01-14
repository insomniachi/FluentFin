using FluentFin.Core.ViewModels;
using Jellyfin.Sdk.Generated.Models;
using Microsoft.UI.Xaml.Controls;

namespace FluentFin.Views.JellyfinSettings;

public sealed partial class UsersPage : Page
{
	public UsersViewModel ViewModel { get; } = App.GetService<UsersViewModel>();

	public UsersPage()
	{
		InitializeComponent();
	}

	private void MenuFlyoutItem_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
	{
		if (ViewModel is null)
		{
			return;
		}

		if (sender is not MenuFlyoutItem item)
		{
			return;
		}

		if (item.CommandParameter is not UserDto user)
		{
			return;
		}

		if (item.Tag is not UserEditorSection section)
		{
			return;
		}

		ViewModel.Navigate(user, section);
	}
}
