using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentFin.Contracts.ViewModels;
using FluentFin.Core.Contracts.Services;
using FluentFin.Core.WebSockets;
using Jellyfin.Sdk.Generated.Models;
using ReactiveUI;

namespace FluentFin.Core.ViewModels;

public partial class HomeViewModel(IJellyfinClient jellyfinClient,
								   IObservable<IInboundSocketMessage> webSocketMessages,
								   GlobalCommands commands) : ObservableObject, INavigationAware
{
	private readonly CompositeDisposable _disposable = [];


	[ObservableProperty]
	public partial ObservableCollection<BaseItemViewModel> ContinueItems { get; set; } = [];

	[ObservableProperty]
	public partial ObservableCollection<BaseItemViewModel> NextUpItems { get; set; } = [];

	[ObservableProperty]
	public partial bool HasNextUpItems { get; set; }

	[ObservableProperty]
	public partial bool HasContinueItems { get; set; }

	[ObservableProperty]
	public partial bool IsLoading { get; set; }

	public ObservableCollection<NamedQueryResult> RecentItems { get; } = [];

	public IJellyfinClient JellyfinClient { get; } = jellyfinClient;

	public Task OnNavigatedFrom()
	{
		_disposable.Dispose();
		return Task.CompletedTask;
	}

	public async Task OnNavigatedTo(object parameter)
	{
		IsLoading = true;

		await Task.WhenAll(UpdateContinueItems(), UpdateNextUpItems(), UpdateRecentItems());

		HasContinueItems = ContinueItems.Count > 0;
		HasNextUpItems = NextUpItems.Count > 0;
		ContinueItems.CollectionChanged += (_, _) => HasContinueItems = ContinueItems.Count > 0;
		NextUpItems.CollectionChanged += (_, _) => HasNextUpItems = NextUpItems.Count > 0;

		webSocketMessages
			.Select(x => x as UserDataChangeMessage)
			.WhereNotNull()
			.ObserveOn(RxApp.MainThreadScheduler)
			.Subscribe(async msg =>
			{
				foreach (var item in msg.Data?.UserDataList ?? [])
				{
					if (!Guid.TryParse(item.ItemId, out var guid))
					{
						continue;
					}

					ProcessContinueWatchingItemChanged(item, guid);
					await ProcessNextUpItemChanged(item, guid);
					ProcessRecentItemChanged(item, guid);
				}
			})
			.DisposeWith(_disposable);

		IsLoading = false;
	}

	private async Task UpdateContinueItems()
	{
		var response = await JellyfinClient.GetContinueWatching();

		if (response is null or { Items: null } or { Items.Count: 0 })
		{
			return;
		}

		ContinueItems = [.. response.Items.Select(BaseItemViewModel.FromDto)];
	}

	private async Task UpdateNextUpItems()
	{
		var response = await JellyfinClient.GetNextUp();

		if (response is null or { Items: null } or { Items.Count: 0 })
		{
			return;
		}

		NextUpItems = [.. response.Items.Select(BaseItemViewModel.FromDto)];
	}

	private async Task UpdateRecentItems()
	{
		await foreach (var list in JellyfinClient.GetRecentItemsFromUserLibraries())
		{
			RecentItems.Add(new(list.Library, [.. list.Items.Select(BaseItemViewModel.FromDto)]));
		}
	}

	private void ProcessRecentItemChanged(WebSockets.Messages.UserItemDataDto userData, Guid guid)
	{
		foreach (var library in RecentItems)
		{
			if (library.Items.FirstOrDefault(x => x.Id == guid) is not BaseItemViewModel { UserData: not null } dto)
			{
				continue;
			}

			dto.UserData.IsFavorite = userData.IsFavorite;
			dto.UserData.Played = userData.Played;
			dto.UserData.PlayedPercentage = userData.PlayedPercentage;
			dto.UserData.UnplayedItemCount = userData.UnplayedItemCount;
		}
		;
	}

	private void ProcessContinueWatchingItemChanged(WebSockets.Messages.UserItemDataDto userData, Guid guid)
	{
		if (ContinueItems.FirstOrDefault(x => x.Id == guid) is not { } item)
		{
			return;
		}

		if (userData.PlaybackPositionTicks is null or 0 ||
			userData.Played == true)
		{
			ContinueItems.Remove(item);
		}
		else
		{
			item.UserData!.PlayedPercentage = userData.PlayedPercentage;
		}
	}

	private async Task ProcessNextUpItemChanged(WebSockets.Messages.UserItemDataDto userData, Guid guid)
	{
		if (NextUpItems.FirstOrDefault(x => x.Id == guid) is not { } item)
		{
			return;
		}

		if (userData.Played == true)
		{
			await UpdateNextUpItems();
		}
	}

	[RelayCommand]
	private async Task Continue() => await PlayFirstItem(ContinueItems);

	[RelayCommand]
	private async Task NextUp() => await PlayFirstItem(NextUpItems);

	private Task PlayFirstItem(ObservableCollection<BaseItemViewModel> items)
	{
		if (items.Count == 0)
		{
			return Task.CompletedTask;
		}

		return commands.PlayDto(items.First().Dto);
	}
}

public record NamedQueryResult(BaseItemDto Library, ObservableCollection<BaseItemViewModel> Items);