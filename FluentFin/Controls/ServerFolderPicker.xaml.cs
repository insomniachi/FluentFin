using System.Collections.ObjectModel;
using CommunityToolkit.WinUI;
using DevWinUI;
using FluentFin.Core.Contracts.Services;
using Jellyfin.Sdk.Generated.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace FluentFin.Controls;

public sealed partial class ServerFolderPicker : UserControl
{
	private readonly IJellyfinClient _jellyfinClient = App.GetService<IJellyfinClient>();

	[GeneratedDependencyProperty(DefaultValue = "/")]
	public partial string CurrentFolder { get; set; }

	public Action? CloseWindow { get; set; }

	public ServerFolderPicker()
	{
		InitializeComponent();

		Loaded += ServerFolderPicker_Loaded;
	}

	public ObservableCollection<FileSystemEntryInfo> Folders { get; } = [];

	private async void ServerFolderPicker_Loaded(object sender, RoutedEventArgs e)
	{
		await Update(CurrentFolder);
	}

	private async void ItemsView_ItemInvoked(ItemsView sender, ItemsViewItemInvokedEventArgs args)
	{
		if (args.InvokedItem is not FileSystemEntryInfo { Path: not null } info)
		{
			return;
		}

		await Update(info.Path);
	}

	private async Task Update(string path)
	{
		var folders = await _jellyfinClient.GetDirectoryContents(path);
		CurrentFolder = path;
		Folders.Clear();
		Folders.AddRange(folders);
	}

	private async void Button_Click(object sender, RoutedEventArgs e)
	{
		var parts = CurrentFolder.Split('/', StringSplitOptions.RemoveEmptyEntries);

		if (parts.Length == 0)
		{
			return;
		}

		await Update($"/{string.Join('/', parts.Take(parts.Length - 1))}");
	}

	private void OnCloseWindow(object sender, RoutedEventArgs e)
	{
		CloseWindow?.Invoke();
	}

	private void OnCancel(object sender, RoutedEventArgs e)
	{
		CurrentFolder = "";
	}
}
