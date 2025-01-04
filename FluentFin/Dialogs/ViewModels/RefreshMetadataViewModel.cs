using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentFin.Core.Contracts.Services;
using Jellyfin.Sdk.Generated.Items.Item.Refresh;

namespace FluentFin.Dialogs.ViewModels;

public partial class RefreshMetadataViewModel(IJellyfinClient jellyfinClient) : ObservableObject, IItemDialogViewModel
{
	public Guid? Item { get; set; }

	public List<string> RefreshModes { get; } = ["Scan for new and updated files", "Search for missing metadata", "Replace all metadata"];

	[ObservableProperty]
	public partial string SelectedRefreshMode { get; set; }

	[ObservableProperty]
	public partial bool ReplaceExistingImages { get; set; }

	[ObservableProperty]
	public partial bool ReplaceExistingTrickplayImages { get; set; }

	public Task Initialize(Guid? item)
	{
		Item = item;
		SelectedRefreshMode = RefreshModes.First();
		return Task.CompletedTask;
	}


	[RelayCommand]
	private async Task Refresh()
	{
		if(Item is null)
		{
			return;
		}

		var imageRefreshMode = SelectedRefreshMode == RefreshModes.First() ? MetadataRefreshMode.Default : MetadataRefreshMode.FullRefresh;
		var metadataRefreshModel = imageRefreshMode;
		var replaceAllMetadata = SelectedRefreshMode == RefreshModes.Last();

		var info = new RefreshMetadataInfo(imageRefreshMode, metadataRefreshModel, ReplaceExistingImages, ReplaceExistingTrickplayImages, replaceAllMetadata);

		await jellyfinClient.RefreshMetadata(Item.Value, info);
	}

}
