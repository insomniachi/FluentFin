using CommunityToolkit.Mvvm.Input;
using FluentFin.Core.Contracts.Services;
using FluentFin.Dialogs.ViewModels;
using FluentFin.Services;
using Jellyfin.Sdk.Generated.Models;
using Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.DataTransfer;

namespace FluentFin.Dialogs;

public partial class DialogCommands(IContentDialogService dialogService,
									IJellyfinClient jellyfinClient)
{
	[RelayCommand]
	private async Task IdentifyDialog(BaseItemDto dto)
	{
		var vm = App.GetService<IdentifyViewModel>();
		await vm.Initialize(dto);

		var result = await dialogService.ShowDialog(vm, x =>
		{
			x.Closing += (_, e) =>
			{
				if (!vm.CanClose)
				{
					e.Cancel = true;
				}
			};
			x.CloseButtonClick += (_, _) => { vm.CanClose = true; };
			x.PrimaryButtonClick += (_, _) => { vm.CanClose = vm.ViewState == IdentifyViewModelState.Result; };
		});
	}

	[RelayCommand]
	private async Task EditMetadataDialog(BaseItemDto dto)
	{
		var vm = App.GetService<EditMetadataViewModel>();
		await vm.Initialize(dto);
		await dialogService.ShowDialog(vm, dialog => CloseOnlyOnCloseAndPrimaryButtonClick(dialog, vm));
	}

	[RelayCommand]
	private async Task AddUserDialog()
	{
		var vm = App.GetService<AddUserViewModel>();
		await vm.Initialize();
		await dialogService.ShowDialog(vm, dialog => CloseOnlyOnCloseAndPrimaryButtonClick(dialog, vm));
	}

	[RelayCommand]
	private static async Task DeleteUser(UserDto user)
	{
		var dialog = new ContentDialog
		{
			XamlRoot = App.MainWindow.Content.XamlRoot,
			Title = "Delete User",
			Content = $"Are you sure you wan to delete the user : {user.Name}?",
			CloseButtonText = "No",
			PrimaryButtonText = "Yes",
			DefaultButton = ContentDialogButton.Primary
		};

		var response = await dialog.ShowAsync();

		if (response == ContentDialogResult.Primary)
		{
			await App.GetService<IJellyfinClient>().DeleteUser(user);
		}
	}

	[RelayCommand]
	private async Task MediaInfoDialog(BaseItemDto dto) => await ShowBaseItemDialog<MediaInfoViewModel>(dto);

	[RelayCommand]
	private async Task EditSubtitlesDialog(BaseItemDto dto) => await ShowBaseItemDialog<EditSubtitlesViewModel>(dto);

	[RelayCommand]
	private async Task RefreshMetadataDialog(Guid id) => await ShowBaseItemDialog<RefreshMetadataViewModel>(id);


	[RelayCommand]
	private async Task EditImagesDialog(BaseItemDto dto)
	{
		var vm = App.GetService<EditImagesViewModel>();
		await vm.Initialize(dto);
		await dialogService.ShowDialog(vm, dialog => CloseOnlyOnCloseButtonClick(dialog, vm));
	}

	[RelayCommand]
	private void CopyUrlToClipboard(BaseItemDto dto)
	{
		if (jellyfinClient.GetStreamUrl(dto) is not { } uri)
		{
			return;
		}

		var package = new DataPackage();
		package.SetText(uri.ToString());
		Clipboard.SetContent(package);
	}

	[RelayCommand]
	private async Task ManageLibraryDialog(VirtualFolderInfo info)
	{
		var vm = App.GetService<ManageLibraryViewModel>();
		await vm.Initialize(info);
		await dialogService.ShowDialog(vm, dialog => CloseOnlyOnCloseAndPrimaryButtonClick(dialog, vm));
	}

	[RelayCommand]
	private async Task CreateLibraryDialog()
	{
		var vm = App.GetService<ManageLibraryViewModel>();
		await vm.Initialize();
		await dialogService.ShowDialog(vm, dialog => CloseOnlyOnCloseAndPrimaryButtonClick(dialog, vm));
	}

	[RelayCommand]
	private async Task QuickConnectAuth()
	{
		var vm = App.GetService<QuickConnectViewModel>();
		await dialogService.ShowDialog(vm, dialog => CloseOnlyOnCloseButtonClick(dialog, vm));
	}

	[RelayCommand]
	private async Task PlayOnSession(BaseItemDto dto)
	{
		var vm = App.GetService<SessionPickerViewModel>();
		await vm.Initialize(dto);
        await dialogService.ShowDialog(vm, dialog => CloseOnlyOnCloseAndPrimaryButtonClick(dialog, vm));
    }

    [RelayCommand]
    private async Task ChangeDefaultSession()
    {
        var vm = App.GetService<SessionPickerViewModel>();
        await vm.Initialize();
        await dialogService.ShowDialog(vm, dialog => CloseOnlyOnCloseAndPrimaryButtonClick(dialog, vm));
    }

	[RelayCommand]
	private async Task SelectSyncPlayGroup()
	{
		var vm = App.GetService<SyncPlayGroupPickerViewModel>();
		await vm.Initialize();
        await dialogService.ShowDialog(vm, dialog => CloseOnAllButtonClick(dialog, vm));
    }


    public static async Task<bool> DeleteLibraryDialog(string name)
	{
		var dialog = new ContentDialog
		{
			XamlRoot = App.MainWindow.Content.XamlRoot,
			Title = "Delete Library",
			Content = $"Are you sure you wan to delete the Library : {name}?",
			CloseButtonText = "No",
			PrimaryButtonText = "Yes",
			DefaultButton = ContentDialogButton.Primary
		};

		var response = await dialog.ShowAsync();

		if (response == ContentDialogResult.Primary)
		{
			await App.GetService<IJellyfinClient>().DeleteLibrary(name);
			return true;
		}

		return false;
	}


	private async Task ShowBaseItemDialog<TViewModel>(BaseItemDto dto)
		where TViewModel : class, IBaseItemDialogViewModel
	{
		var vm = App.GetService<TViewModel>();
		await vm.Initialize(dto);
		await dialogService.ShowDialog(vm, null!);
	}

	private async Task ShowBaseItemDialog<TViewModel>(Guid? id)
	where TViewModel : class, IItemDialogViewModel
	{
		var vm = App.GetService<TViewModel>();
		await vm.Initialize(id);
		await dialogService.ShowDialog(vm, null!);
	}

	private static void CloseOnlyOnCloseButtonClick(ContentDialog dialog, IHandleClose vm)
	{
		dialog.Closing += (_, e) =>
		{
			if (!vm.CanClose)
			{
				e.Cancel = true;
			}
		};
		dialog.CloseButtonClick += (_, _) => { vm.CanClose = true; };
	}
	private static void CloseOnlyOnCloseAndPrimaryButtonClick(ContentDialog dialog, IHandleClose vm)
	{
		dialog.Closing += (_, e) =>
		{
			if (!vm.CanClose)
			{
				e.Cancel = true;
			}
		};
		dialog.CloseButtonClick += (_, _) => { vm.CanClose = true; };
		dialog.PrimaryButtonClick += (_, _) => { vm.CanClose = true; };
	}

	private static void CloseOnAllButtonClick(ContentDialog dialog, IHandleClose vm)
	{
        dialog.Closing += (_, e) =>
        {
            if (!vm.CanClose)
            {
                e.Cancel = true;
            }
        };
        dialog.CloseButtonClick += (_, _) => { vm.CanClose = true; };
        dialog.PrimaryButtonClick += (_, _) => { vm.CanClose = true; };
        dialog.SecondaryButtonClick += (_, _) => { vm.CanClose = true; };
    }
}
