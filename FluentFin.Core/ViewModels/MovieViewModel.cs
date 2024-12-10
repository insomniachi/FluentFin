using CommunityToolkit.Mvvm.ComponentModel;
using FluentFin.Contracts.ViewModels;
using FluentFin.Core.Contracts.Services;
using Jellyfin.Sdk.Generated.Models;

namespace FluentFin.Core.ViewModels;

public partial class MovieViewModel(IJellyfinClient jellyfinClient) : ObservableObject, INavigationAware
{
	public Task OnNavigatedFrom() => Task.CompletedTask;

	public async Task OnNavigatedTo(object parameter)
	{
		if(parameter is not BaseItemDto dto)
		{
			return;
		}

		if(dto.Id is not { } id)
		{
			return;
		}

		var full = await jellyfinClient.GetItem(id);

		if(full is null)
		{
			return;
		}

		Dto = full;

		var response = await jellyfinClient.GetSimilarItems(Dto);

		if(response is null or { Items : null })
		{
			return;
		}

		Similar = response.Items;

	}

	[ObservableProperty]
	public partial BaseItemDto Dto { get; set; }

	[ObservableProperty]
	public partial List<BaseItemDto> Similar { get; set; }
}
