using CommunityToolkit.Mvvm.ComponentModel;
using FluentFin.Contracts.ViewModels;
using FluentFin.Core.Contracts.Services;
using FluentFin.Core.WebSockets;
using Jellyfin.Sdk.Generated.Models;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace FluentFin.Core.ViewModels
{
	public partial class HomeViewModel(IJellyfinClient jellyfinClient,
									   IObservable<IInboundSocketMessage> webSocketMessages) : ObservableObject, INavigationAware
	{
		private readonly CompositeDisposable _disposable = new();


		[ObservableProperty]
		public partial ObservableCollection<BaseItemDto> ContinueItems { get; set; } = [];

		[ObservableProperty]
		public partial ObservableCollection<BaseItemDto> NextUpItems { get; set; } = [];

		[ObservableProperty]
		public partial bool HasNextUpItems { get; set; }

		[ObservableProperty]
		public partial bool HasContinueItems { get; set; }

		public ObservableCollection<NamedDtoQueryResult> RecentItems { get; } = [];

		public string BaseUrl { get; } = jellyfinClient.BaseUrl;

		public Task OnNavigatedFrom()
		{
			_disposable.Dispose();
			return Task.CompletedTask;
		}

		public async Task OnNavigatedTo(object parameter)
		{
			await Task.WhenAll(UpdateContinueItems(), UpdateNextUpItems(), UpdateRecentItems());

			HasContinueItems = ContinueItems.Count > 0;
			HasNextUpItems = NextUpItems.Count > 0;
			ContinueItems.CollectionChanged += (_, _) => HasContinueItems = ContinueItems.Count > 0;
			NextUpItems.CollectionChanged += (_, _) => HasNextUpItems = NextUpItems.Count > 0;

			webSocketMessages
				.Select(x => x as UserDataChangeMessage)
				.WhereNotNull()
				.ObserveOn(RxApp.MainThreadScheduler)
				.Subscribe(msg =>
				{
					foreach (var item in msg.Data?.UserDataList ?? [])
					{
						if (!Guid.TryParse(item.ItemId, out var guid))
						{
							continue;
						}

						ProcessContinueWatchingItemChanged(item, guid);
						ProcessNextUpItemChanged(item, guid);
						ProcessRecentItemChanged(item, guid);
					}
				})
				.DisposeWith(_disposable);

		}

		private async Task UpdateContinueItems()
		{
			var response = await jellyfinClient.GetContinueWatching();

			if (response is null or { Items: null } or { Items.Count: 0 })
			{
				return;
			}

			ContinueItems = new(response.Items);
		}

		private async Task UpdateNextUpItems()
		{
			var response = await jellyfinClient.GetNextUp();

			if (response is null or { Items: null } or { Items.Count: 0 })
			{
				return;
			}

			NextUpItems = new(response.Items);
		}

		private async Task UpdateRecentItems()
		{
			await foreach (var list in jellyfinClient.GetRecentItemsFromUserLibraries())
			{
				RecentItems.Add(new(list.Name, list.Items));
			}
		}

		private void ProcessRecentItemChanged(WebSockets.Messages.UserItemDataDto userData, Guid guid)
		{
			foreach (var library in RecentItems)
			{
				if (library.Items.FirstOrDefault(x => x.Id == guid) is not BaseItemDto { UserData: not null } dto)
				{
					continue;
				}

				var index = library.Items.IndexOf(dto);
				library.Items.RemoveAt(index);

				dto.UserData.IsFavorite = userData.IsFavorite;
				dto.UserData.Played = userData.Played;
				dto.UserData.PlayedPercentage = userData.PlayedPercentage;
				dto.UserData.UnplayedItemCount = userData.UnplayedItemCount;
				library.Items.Insert(index, dto);
			};
		}

		private void ProcessContinueWatchingItemChanged(WebSockets.Messages.UserItemDataDto userData, Guid guid)
		{
			if (ContinueItems.FirstOrDefault(x => x.Id == guid) is not { } item)
			{
				return;
			}

			if (userData.PlaybackPositionTicks is null or 0)
			{
				ContinueItems.Remove(item);
			}
		}

		private void ProcessNextUpItemChanged(WebSockets.Messages.UserItemDataDto userData, Guid guid)
		{
			if (NextUpItems.FirstOrDefault(x => x.Id == guid) is not { } item)
			{
				return;
			}

			if (userData.Played == true)
			{
				NextUpItems.Remove(item);
			}
		}
	}
}
