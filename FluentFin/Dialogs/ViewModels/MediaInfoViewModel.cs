using CommunityToolkit.Mvvm.ComponentModel;
using FluentFin.Core.Contracts.Services;
using Jellyfin.Sdk.Generated.Models;

namespace FluentFin.Dialogs.ViewModels;

public partial class MediaInfoViewModel(IJellyfinClient jellyfinClient) : ObservableObject, IBaseItemDialogViewModel
{
	[ObservableProperty]
	public partial BaseItemDto? Item { get; set; }

	public async Task Initialize(BaseItemDto item)
	{
		if (item.Id is not { } id)
		{
			return;
		}

		Item = await jellyfinClient.GetItem(id);
	}
}
