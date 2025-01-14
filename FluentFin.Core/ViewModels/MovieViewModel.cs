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
		if (parameter is not BaseItemDto dto)
		{
			return;
		}

		if (dto.Id is not { } id)
		{
			return;
		}

		await UpdateMovie(id);
		await UpdateSimilar(dto);
	}

	[ObservableProperty]
	public partial BaseItemDto Dto { get; set; }

	[ObservableProperty]
	public partial List<BaseItemDto> Similar { get; set; }

	private async Task UpdateMovie(Guid id)
	{
		var movie = await jellyfinClient.GetItem(id);
		if (movie is null)
		{
			return;
		}

		Dto = movie;

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
}
