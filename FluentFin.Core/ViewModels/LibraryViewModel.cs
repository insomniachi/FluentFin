using System.Collections.ObjectModel;
using System.Reactive.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using DynamicData.Binding;
using FluentFin.Contracts.ViewModels;
using FluentFin.Core.Contracts.Services;
using Jellyfin.Sdk.Generated.Models;
using ReactiveUI;

namespace FluentFin.Core.ViewModels;

public partial class LibraryViewModel : ObservableObject, INavigationAware
{
	private readonly SourceCache<BaseItemViewModel, Guid> _itemsCache = new(x => x.Id ?? Guid.Empty);
	private readonly ReadOnlyObservableCollection<BaseItemViewModel> _items;


	public LibraryViewModel(IJellyfinClient jellyfinClient)
	{
		JellyfinClient = jellyfinClient;

		var pageRequest = this.WhenAnyValue(x => x.SelectedPage).Select(page => new PageRequest(page + 1, 100));
		var comparer = this.WhenAnyValue(x => x.SortBy, x => x.Order)
						   .Select(GetComparer);

		var filter = this.WhenAnyValue(x => x.Filter)
			.WhereNotNull()
			.SelectMany(x => x.WhenAnyPropertyChanged())
			.Where(x => x is not null)
			.Select(x => (Func<BaseItemViewModel, bool>)x!.IsVisible);

		Filter.WhenAnyPropertyChanged()
			.Subscribe(x =>
			{
				_itemsCache.Refresh();
				UpdateNumberOfPages();
			});

		_itemsCache
			.Connect()
			.RefCount()
			.Filter(Filter.IsVisible)
			.SortAndPage(comparer, pageRequest)
			.Bind(out _items)
			.Subscribe();
	}

	private IComparer<BaseItemViewModel> GetComparer((ItemSortBy, SortOrder) sortDescription)
	{
		return sortDescription switch
		{
			(ItemSortBy.Name, SortOrder.Ascending) => SortExpressionComparer<BaseItemViewModel>.Ascending(x => x.Name ?? ""),
			(ItemSortBy.CommunityRating, SortOrder.Ascending) => SortExpressionComparer<BaseItemViewModel>.Ascending(x => x.CommunityRating ?? 0),
			(ItemSortBy.CriticRating, SortOrder.Ascending) => SortExpressionComparer<BaseItemViewModel>.Ascending(x => x.CriticRating ?? 0),
			(ItemSortBy.DateCreated, SortOrder.Ascending) => SortExpressionComparer<BaseItemViewModel>.Ascending(x => x.DateCreated ?? new DateTimeOffset()),
			(ItemSortBy.DatePlayed, SortOrder.Ascending) => SortExpressionComparer<BaseItemViewModel>.Ascending(x => x.UserData?.LastPlayedDate ?? new DateTimeOffset()),
			(ItemSortBy.PlayCount, SortOrder.Ascending) => SortExpressionComparer<BaseItemViewModel>.Ascending(x => x.UserData?.PlayCount ?? 0),
			(ItemSortBy.PremiereDate, SortOrder.Ascending) => SortExpressionComparer<BaseItemViewModel>.Ascending(x => x.PremiereDate ?? new DateTimeOffset()),
			(ItemSortBy.Runtime, SortOrder.Ascending) => SortExpressionComparer<BaseItemViewModel>.Ascending(x => x.RunTimeTicks ?? 0),

			(ItemSortBy.Name, SortOrder.Descending) => SortExpressionComparer<BaseItemViewModel>.Descending(x => x.Name ?? ""),
			(ItemSortBy.CommunityRating, SortOrder.Descending) => SortExpressionComparer<BaseItemViewModel>.Descending(x => x.CommunityRating ?? 0),
			(ItemSortBy.CriticRating, SortOrder.Descending) => SortExpressionComparer<BaseItemViewModel>.Descending(x => x.CriticRating ?? 0),
			(ItemSortBy.DateCreated, SortOrder.Descending) => SortExpressionComparer<BaseItemViewModel>.Descending(x => x.DateCreated ?? new DateTimeOffset()),
			(ItemSortBy.DatePlayed, SortOrder.Descending) => SortExpressionComparer<BaseItemViewModel>.Descending(x => x.UserData?.LastPlayedDate ?? new DateTimeOffset()),
			(ItemSortBy.PlayCount, SortOrder.Descending) => SortExpressionComparer<BaseItemViewModel>.Descending(x => x.UserData?.PlayCount ?? 0),
			(ItemSortBy.PremiereDate, SortOrder.Descending) => SortExpressionComparer<BaseItemViewModel>.Descending(x => x.PremiereDate ?? new DateTimeOffset()),
			(ItemSortBy.Runtime, SortOrder.Descending) => SortExpressionComparer<BaseItemViewModel>.Descending(x => x.RunTimeTicks ?? 0),

			_ => SortExpressionComparer<BaseItemViewModel>.Ascending(x => x.Name ?? ""),
		};
	}

	public ReadOnlyObservableCollection<BaseItemViewModel> Items => _items;

	[ObservableProperty]
	public partial int NumberOfPages { get; set; }

	[ObservableProperty]
	public partial int SelectedPage { get; set; }

	[ObservableProperty]
	public partial ItemSortBy SortBy { get; set; } = ItemSortBy.Name;

	[ObservableProperty]
	public partial SortOrder Order { get; set; } = SortOrder.Ascending;

	[ObservableProperty]
	public partial List<string> TagsSource { get; set; } = new();

	[ObservableProperty]
	public partial List<string> GenresSource { get; set; } = new();

	[ObservableProperty]
	public partial List<string> OfficialRatingsSource { get; set; } = new();

	[ObservableProperty]
	public partial List<string> YearsSource { get; set; } = new();

	[ObservableProperty]
	public partial bool IsLoading { get; set; }

	public IJellyfinClient JellyfinClient { get; }

	public LibraryFilter Filter { get; set; } = new();

	[RelayCommand]
	public void UpdateSortBy(ItemSortBy sortBy) => SortBy = sortBy;

	[RelayCommand]
	public void UpdateSortOrder(SortOrder order) => Order = order;


	public Task OnNavigatedFrom() => Task.CompletedTask;

	public async Task OnNavigatedTo(object parameter)
	{
		if (parameter is BaseItemDto libraryDto)
		{
			await Initialize(libraryDto);
		}
		else if(parameter is Guid id)
		{
			var dto = await JellyfinClient.GetItem(id);
			if(dto is null)
			{
				return;
			}
			await Initialize(dto);
		}
	}

	private async Task Initialize(BaseItemDto dto)
	{
		IsLoading = true;

		var result = await JellyfinClient.GetItems(dto);

		if (result is null or { Items: null })
		{
			return;
		}

		_itemsCache.AddOrUpdate(result.Items.Select(BaseItemViewModel.FromDto));
		UpdateNumberOfPages();

		var filters = await JellyfinClient.GetFilters(dto);

		if (filters is null)
		{
			return;
		}

		TagsSource = filters.Tags?.ToList() ?? [];
		GenresSource = filters.Genres?.ToList() ?? [];
		OfficialRatingsSource = filters.OfficialRatings?.ToList() ?? [];
		YearsSource = filters.Years?.Where(x => x.HasValue).Select(x => x!.Value.ToString()).ToList() ?? [];

		IsLoading = false;
	}

	private void UpdateNumberOfPages()
	{
		NumberOfPages = Filter.IsEmptyFilter()
			? (int)Math.Ceiling(_itemsCache.Items.Count / 100d)
			: (int)Math.Ceiling(Items.Count / 100d);
	}
}

public partial class LibraryFilter : ObservableObject
{
	public LibraryFilter()
	{
		Tags.CollectionChanged += (_, _) => OnPropertyChanged(nameof(Tags));
		Genres.CollectionChanged += (_, _) => OnPropertyChanged(nameof(Genres));
		OfficialRatings.CollectionChanged += (_, _) => OnPropertyChanged(nameof(OfficialRatings));
		Years.CollectionChanged += (_, _) => OnPropertyChanged(nameof(Years));
	}

	public ObservableCollection<string> Tags { get; set; } = [];

	[ObservableProperty]
	public partial ObservableCollection<string> Genres { get; set; } = new();

	[ObservableProperty]
	public partial ObservableCollection<string> OfficialRatings { get; set; } = new();

	[ObservableProperty]
	public partial ObservableCollection<string> Years { get; set; } = new();

	public bool IsEmptyFilter() => this is { Tags.Count: 0, Genres.Count: 0, OfficialRatings.Count: 0, Years.Count: 0 };

	public bool IsVisible(BaseItemViewModel dto)
	{
		bool hasMatchingTags = true;
		bool hasMatchingGenres = true;
		bool hasMatchingOfficialRatings = true;
		bool hasMatchingYears = true;

		if (Tags is { Count: > 0 })
		{
			hasMatchingTags = dto.Tags?.Intersect(Tags).Count() == Tags.Count;
		}

		if (Genres is { Count: > 0 })
		{
			hasMatchingGenres = dto.Genres?.Intersect(Genres).Count() == Genres.Count;
		}

		if (OfficialRatings is { Count: > 0 })
		{
			hasMatchingOfficialRatings = OfficialRatings.Contains(dto.OfficialRating ?? "");
		}

		if (dto.ProductionYear is > 0 && Years is { Count: > 0 })
		{
			hasMatchingYears = Years.Contains(dto.ProductionYear.Value.ToString());
		}

		return hasMatchingTags && hasMatchingGenres && hasMatchingOfficialRatings && hasMatchingYears;
	}
}
