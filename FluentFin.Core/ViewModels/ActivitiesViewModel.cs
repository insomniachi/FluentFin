using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DynamicData;
using FluentFin.Contracts.ViewModels;
using FluentFin.Core.Contracts.Services;
using Jellyfin.Sdk.Generated.Models;

namespace FluentFin.Core.ViewModels;

public partial class ActivitiesViewModel : ObservableObject, INavigationAware
{
	private readonly IJellyfinClient _jellyfinClient;
	private readonly SourceCache<ActivityLogEntry, long> _itemsCache = new(x => x.Id ?? 0);
	private readonly ReadOnlyObservableCollection<ActivityLogEntry> _items;

	public ActivitiesViewModel(IJellyfinClient jellyfinClient)
	{
		_jellyfinClient = jellyfinClient;

		_itemsCache
			.Connect()
			.RefCount()
			.Bind(out _items)
			.Subscribe();
	}

	public ReadOnlyObservableCollection<ActivityLogEntry> Items => _items;


	public Task OnNavigatedFrom() => Task.CompletedTask;

	public async Task OnNavigatedTo(object parameter)
	{
		var activities = await _jellyfinClient.GetAllActivities();

		if (activities is not { Items.Count: > 0 })
		{
			return;
		}

		_itemsCache.AddOrUpdate(activities.Items);
	}

}
