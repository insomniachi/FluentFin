using CommunityToolkit.Mvvm.ComponentModel;
using FluentFin.Contracts.ViewModels;
using FluentFin.Core.Contracts.Services;
using Jellyfin.Sdk.Generated.Models;

namespace FluentFin.Core.ViewModels;

public partial class SeriesViewModel(IJellyfinClient jellyfinClient) : ObservableObject, INavigationAware
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

		if (dto.Type is not BaseItemDto_Type.Series)
		{
			return;
		}

		await UpdateSeries(id);
		await UpdateNextUp(dto);
		await UpdateSeasons(dto);
		await UpdateSimilar(dto);

	}

	[ObservableProperty]
	public partial BaseItemDto Dto { get; set; }

	[ObservableProperty]
	public partial List<BaseItemDto> Similar { get; set; }

	[ObservableProperty]
	public partial List<BaseItemDto> Seasons { get; set; }

	[ObservableProperty]
	public partial BaseItemDto NextUp { get; set; }

	private async Task UpdateSeries(Guid id)
	{
		var series = await jellyfinClient.GetItem(id);
		if (series is null)
		{
			return;
		}

		Dto = series;

	}

	private async Task UpdateNextUp(BaseItemDto dto)
	{
		var response = await jellyfinClient.GetNextUp(dto);

		if (response is null or { Items: null } or { Items.Count: 0 })
		{
			return;
		}

		NextUp = response.Items[0];
	}

	private async Task UpdateSimilar(BaseItemDto dto)
	{
		var response = await jellyfinClient.GetSimilarItems(dto);

		if (response is null or { Items: null })
		{
			return;
		}

		Similar = response.Items;
	}

	private async Task UpdateSeasons(BaseItemDto dto)
	{
		var response = await jellyfinClient.GetItems(dto);

		if (response is null or { Items: null })
		{
			return;
		}

		Seasons = response.Items;
	}
}
