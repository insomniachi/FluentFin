using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using DynamicData.Binding;
using FluentFin.Contracts.ViewModels;
using FluentFin.Core.Contracts.Services;
using Jellyfin.Sdk.Generated.Models;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Reactive.Linq;

namespace FluentFin.Core.ViewModels;

public partial class LibraryViewModel : ObservableObject, INavigationAware
{
	private readonly IJellyfinClient _jellyfinClient;
	private readonly SourceCache<BaseItemDto, Guid> _itemsCache = new(x => x.Id ?? Guid.Empty);
	private readonly ReadOnlyObservableCollection<BaseItemDto> _items;


	public LibraryViewModel(IJellyfinClient jellyfinClient)
	{
		_jellyfinClient = jellyfinClient;

		var pageRequest = this.WhenAnyValue(x => x.SelectedPage).Select(page => new PageRequest(page + 1, 100));
		var comparer = this.WhenAnyValue(x => x.SortBy, x => x.Order)
						   .Select(GetComparer);

		_itemsCache
			.Connect()
			.RefCount()
			.SortAndPage(comparer, pageRequest)
			.Bind(out _items)
			.Subscribe();
	}

	private IComparer<BaseItemDto> GetComparer((ItemSortBy, SortOrder) sortDescription)
	{
		return sortDescription switch
		{
			(ItemSortBy.Name, SortOrder.Ascending) => SortExpressionComparer<BaseItemDto>.Ascending(x => x.Name ?? ""),
			(ItemSortBy.CommunityRating, SortOrder.Ascending) => SortExpressionComparer<BaseItemDto>.Ascending(x => x.CommunityRating ?? 0),
			(ItemSortBy.CriticRating, SortOrder.Ascending) => SortExpressionComparer<BaseItemDto>.Ascending(x => x.CriticRating ?? 0),
			(ItemSortBy.DateCreated, SortOrder.Ascending) => SortExpressionComparer<BaseItemDto>.Ascending(x => x.DateCreated ?? new DateTimeOffset()),
			(ItemSortBy.DatePlayed, SortOrder.Ascending) => SortExpressionComparer<BaseItemDto>.Ascending(x => x.UserData?.LastPlayedDate ?? new DateTimeOffset()),
			(ItemSortBy.PlayCount, SortOrder.Ascending) => SortExpressionComparer<BaseItemDto>.Ascending(x => x.UserData?.PlayCount ?? 0),
			(ItemSortBy.PremiereDate, SortOrder.Ascending) => SortExpressionComparer<BaseItemDto>.Ascending(x => x.PremiereDate ?? new DateTimeOffset()),
			(ItemSortBy.Runtime, SortOrder.Ascending) => SortExpressionComparer<BaseItemDto>.Ascending(x => x.RunTimeTicks ?? 0),

			(ItemSortBy.Name, SortOrder.Descending) => SortExpressionComparer<BaseItemDto>.Descending(x => x.Name ?? ""),
			(ItemSortBy.CommunityRating, SortOrder.Descending) => SortExpressionComparer<BaseItemDto>.Descending(x => x.CommunityRating ?? 0),
			(ItemSortBy.CriticRating, SortOrder.Descending) => SortExpressionComparer<BaseItemDto>.Descending(x => x.CriticRating ?? 0),
			(ItemSortBy.DateCreated, SortOrder.Descending) => SortExpressionComparer<BaseItemDto>.Descending(x => x.DateCreated ?? new DateTimeOffset()),
			(ItemSortBy.DatePlayed, SortOrder.Descending) => SortExpressionComparer<BaseItemDto>.Descending(x => x.UserData?.LastPlayedDate ?? new DateTimeOffset()),
			(ItemSortBy.PlayCount, SortOrder.Descending) => SortExpressionComparer<BaseItemDto>.Descending(x => x.UserData?.PlayCount ?? 0),
			(ItemSortBy.PremiereDate, SortOrder.Descending) => SortExpressionComparer<BaseItemDto>.Descending(x => x.PremiereDate ?? new DateTimeOffset()),
			(ItemSortBy.Runtime, SortOrder.Descending) => SortExpressionComparer<BaseItemDto>.Descending(x => x.RunTimeTicks ?? 0),

			_ => SortExpressionComparer<BaseItemDto>.Ascending(x => x.Name ?? ""),
		};
	}

	public ReadOnlyObservableCollection<BaseItemDto> Items => _items;

	[ObservableProperty]
	public partial int NumberOfPages { get; set; }

	[ObservableProperty]
	public partial int SelectedPage { get; set; }

	[ObservableProperty]
	public partial ItemSortBy SortBy { get; set; } = ItemSortBy.Name;

	[ObservableProperty]
	public partial SortOrder Order { get; set; } = SortOrder.Ascending;

	[RelayCommand]
	public void UpdateSortBy(ItemSortBy sortBy) => SortBy = sortBy;

	[RelayCommand]
	public void UpdateSortOrder(SortOrder order) => Order = order;

	public Task OnNavigatedFrom() => Task.CompletedTask;

	public async Task OnNavigatedTo(object parameter)
	{
		if (parameter is not BaseItemDto libraryDto)
		{
			return;
		}

		var result = await _jellyfinClient.GetItems(libraryDto);

		if(result is null or { Items : null })
		{
			return;
		}

		NumberOfPages = (int)Math.Ceiling(result.Items.Count / 100d);
		_itemsCache.AddOrUpdate(result.Items);
	}
}
