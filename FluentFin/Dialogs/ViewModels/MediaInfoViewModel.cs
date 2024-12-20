using CommunityToolkit.Mvvm.ComponentModel;
using FluentFin.Core.Contracts.Services;
using Jellyfin.Sdk.Generated.Models;

namespace FluentFin.Dialogs.ViewModels;

public partial class MediaInfoViewModel(IJellyfinClient jellyfinClient) : ObservableObject
{
	[ObservableProperty]
	public partial BaseItemDto Item { get; set; }

	public async Task Initialize(Guid id)
	{
		var item = await jellyfinClient.GetItem(id);

		if(item is null)
		{
			return;
		}

		Item = item;
	}
}
