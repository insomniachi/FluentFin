using FluentFin.Core.Contracts.Services;
using Jellyfin.Sdk.Generated.Models;
using Windows.UI.StartScreen;

namespace FluentFin.Services;

public class JumpListService : IJumpListService
{
	public async Task Initialize(IEnumerable<BaseItemDto> libaries)
	{
		var jumplist = await JumpList.LoadCurrentAsync();
		var itemsAdded = false;
		foreach (var item in libaries)
		{
			var id = item.Id?.ToString();

			if (string.IsNullOrEmpty(id))
			{
				continue;
			}

			if(jumplist.Items.FirstOrDefault(x => x.Arguments == id) is { } existing)
			{
				continue;
			}

			var jumplistItem = JumpListItem.CreateWithArguments(id, item.Name);
			jumplistItem.GroupName = "Libraries";
			jumplist.Items.Add(jumplistItem);
			itemsAdded = true;
		}

		if(itemsAdded)
		{
			await jumplist.SaveAsync();
		}
	}
}
