using CommunityToolkit.Mvvm.ComponentModel;
using FluentFin.Contracts.ViewModels;
using FluentFin.Core.Contracts.Services;
using Jellyfin.Sdk.Generated.Models;
using System.Collections.ObjectModel;

namespace FluentFin.Core.ViewModels
{
	public partial class HomeViewModel(IJellyfinClient jellyfinClient) : ObservableObject, INavigationAware
	{

		[ObservableProperty]
		public partial List<BaseItemDto> ContinueItems { get; set; } = [];

		[ObservableProperty]
		public partial List<BaseItemDto> NextUpItems { get; set; } = [];

		public ObservableCollection<NamedDtoQueryResult> RecentItems { get; } = [];

		public string BaseUrl { get; } = jellyfinClient.BaseUrl;

		public Task OnNavigatedFrom() => Task.CompletedTask;

		public async Task OnNavigatedTo(object parameter)
		{
			await Task.WhenAll(UpdateContinueItems(), UpdateNextUpItems(), UpdateRecentItems());

			//await jellyfinClient.BitrateTest();
		}

		private async Task UpdateContinueItems()
		{
			var response = await jellyfinClient.GetContinueWatching();

			if (response is null or { Items: null } or { Items.Count: 0 })
			{
				return;
			}

			ContinueItems = response.Items;
		}

		private async Task UpdateNextUpItems()
		{
			var response = await jellyfinClient.GetNextUp();

			if (response is null or { Items: null } or { Items.Count: 0 })
			{
				return;
			}

			NextUpItems = response.Items;
		}

		private async Task UpdateRecentItems()
		{
			await foreach (var list in jellyfinClient.GetRecentItemsFromUserLibraries())
			{
				RecentItems.Add(new(list.Name, list.Items));
			}
		}
	}
}
