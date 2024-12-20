using CommunityToolkit.Mvvm.Input;
using FluentFin.Core.Contracts.Services;
using FluentFin.Dialogs.ViewModels;
using FluentFin.Services;
using Jellyfin.Sdk.Generated.Models;
using Windows.ApplicationModel.DataTransfer;

namespace FluentFin.Dialogs;

public partial class DialogCommands(IContentDialogService dialogService,
									IJellyfinClient jellyfinClient)
{
	[RelayCommand]
	private async Task IdentifyDialog(BaseItemDto dto)
	{
		var vm = App.GetService<IdentifyViewModel>();
		vm.Item = dto;

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
			x.PrimaryButtonClick += (_, _) => { vm.CanClose = vm.ViewState == State.Result; };
		});
	}

	[RelayCommand]
	private async Task EditMetadataDialog(BaseItemDto dto)
	{
		var vm = App.GetService<EditMetadataViewModel>();
		await vm.Initialize(dto.Id ?? Guid.Empty);
		await dialogService.ShowDialog(vm, x =>
		{
			x.Closing += (_, e) =>
			{
				if (!vm.CanClose)
				{
					e.Cancel = true;
				}
			};
			x.CloseButtonClick += (_, _) => { vm.CanClose = true; };
			x.PrimaryButtonClick += (_, _) => { vm.CanClose = true; };
		});
	}

	[RelayCommand]
	private async Task MediaInfoDialog(BaseItemDto dto)
	{
		var vm = App.GetService<MediaInfoViewModel>();
		await vm.Initialize(dto.Id ?? Guid.Empty);
		await dialogService.ShowDialog(vm, null!);
	}

	[RelayCommand]
	private void CopyUrlToClipboard(BaseItemDto dto)
	{
		if(jellyfinClient.GetStreamUrl(dto) is not { } uri)
		{
			return;
		}

		var package = new DataPackage();
		package.SetText(uri.ToString());
		Clipboard.SetContent(package);
	}
}
