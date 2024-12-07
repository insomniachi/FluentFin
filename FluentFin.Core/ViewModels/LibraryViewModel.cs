using CommunityToolkit.Mvvm.ComponentModel;
using DynamicData;
using DynamicData.Binding;
using FluentFin.Contracts.ViewModels;
using FluentFin.Core.Contracts.Services;
using Jellyfin.Client.Models;
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

		_itemsCache
			.Connect()
			.RefCount()
			.SortAndPage(SortExpressionComparer<BaseItemDto>.Ascending(x => x.Name ?? ""), pageRequest)
			.Bind(out _items)
			.DisposeMany()
			.Subscribe();
	}

	public ReadOnlyObservableCollection<BaseItemDto> Items => _items;

	[ObservableProperty]
	public partial int NumberOfPages { get; set; }

	[ObservableProperty]
	public partial int SelectedPage { get; set; }

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
