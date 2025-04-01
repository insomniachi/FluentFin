using FluentFin.Core.Contracts.Services;
using FluentFin.Core.ViewModels;
using FluentFin.Dialogs;
using Jellyfin.Sdk.Generated.Models;
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

	private async void OnDelete(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
	{
		if (sender is not MenuFlyoutItem item)
		{
			return;
		}

		if (item.Tag is not VirtualFolderInfo info)
		{
			return;
		}

		var delete = await DialogCommands.DeleteLibraryDialog(info.Name ?? "");

		if (delete)
		{
			ViewModel.VirtualFolders.Remove(info);
		}
	}

	private async void OnRename(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
	{
		if (sender is not MenuFlyoutItem item)
		{
			return;
		}

		if (item.Tag is not VirtualFolderInfo info)
		{
			return;
		}

		var newName = await App.GetService<IUserInput<string>>().GetValue();

		if (!string.IsNullOrEmpty(newName))
		{
			await App.GetService<IJellyfinClient>().RenameLibrary(info.Name ?? "", newName);
			await ViewModel.OnNavigatedTo(new());
		}
	}
}
