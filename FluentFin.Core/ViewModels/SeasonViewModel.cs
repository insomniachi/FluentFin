using CommunityToolkit.Mvvm.ComponentModel;
using FluentFin.Contracts.ViewModels;
using FluentFin.Core.Contracts.Services;
using Jellyfin.Sdk.Generated.Models;

namespace FluentFin.Core.ViewModels;

public partial class SeasonViewModel(IJellyfinClient jellyfinClient) : ObservableObject, INavigationAware
{
	public Task OnNavigatedFrom() => Task.CompletedTask;

	public async Task OnNavigatedTo(object parameter)
	{
		if (parameter is not BaseItemDto dto)
		{
			return;
		}

		if (dto.Id is not { } id)
		{
			return;
		}

		if (dto.Type is not BaseItemDto_Type.Season)
		{
			return;
		}

		await UpdateSeason(id);
		await UpdateEpisodes(dto);
	}

	[ObservableProperty]
	public partial BaseItemDto Dto { get; set; }

	[ObservableProperty]
	public partial List<BaseItemDto> Episodes { get; set; }


	private async Task UpdateSeason(Guid id)
	{
		var series = await jellyfinClient.GetItem(id);
		if (series is null)
		{
			return;
		}

		Dto = series;
	}

	private async Task UpdateEpisodes(BaseItemDto dto)
	{
		var response = await jellyfinClient.GetItems(dto);

		if (response is null or { Items: null })
		{
			return;
		}

		Episodes = response.Items;
	}
}
