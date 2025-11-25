using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Reactive.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentFin.Core.Contracts.Services;
using Jellyfin.Sdk.Generated.Models;
using ReactiveUI;

namespace FluentFin.Core.ViewModels;

public partial class UserParentalControlEditorViewModel(IJellyfinClient jellyfinClient,
														IUserInput<AccessSchedule> accessSchedulePicker) : UserSectionEditorViewModel
{
	[ObservableProperty]
	public partial List<ParentalRating?> Ratings { get; set; } = [];

	[ObservableProperty]
	public partial ParentalRating? MaximumAllowedRating { get; set; }

	[ObservableProperty]
	public partial ObservableCollection<string> AllowedTags { get; set; } = [];

	[ObservableProperty]
	public partial ObservableCollection<string> BlockedTags { get; set; } = [];

	public ObservableCollection<AccessSchedule> AccessSchedules { get; set; } = [];

	public ObservableCollection<UnratedItemViewModel> BlockUnratedItems { get; } = [];


	protected override async Task Initialize(UserDto user)
	{
		if (user?.Policy is not { } policy)
		{
			return;
		}

		Ratings = [null, .. await jellyfinClient.GetParentalRatings()];
		MaximumAllowedRating = Ratings.FirstOrDefault(x => x?.Value == policy.MaxParentalRating);
		AllowedTags = [.. policy.AllowedTags ?? []];
		BlockedTags = [.. policy.BlockedTags ?? []];
		AccessSchedules = [.. policy.AccessSchedules ?? []];

		BlockUnratedItems.Clear();
		foreach (var value in Enum.GetValues<UnratedItem>())
		{
			BlockUnratedItems.Add(new UnratedItemViewModel(user)
			{
				Item = value,
				IsSelected = policy.BlockUnratedItems?.Contains(value) == true
			});
		}

		AllowedTags.CollectionChanged += AllowedTags_CollectionChanged;
		BlockedTags.CollectionChanged += BlockedTags_CollectionChanged;
		AccessSchedules.CollectionChanged += AccessSchedules_CollectionChanged;

		this.WhenAnyValue(x => x.MaximumAllowedRating).Subscribe(rating => policy.MaxParentalRating = rating?.Value);
	}

	private void AccessSchedules_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		if (User?.Policy is not { } policy)
		{
			return;
		}

		if (e.Action == NotifyCollectionChangedAction.Add)
		{
			foreach (var item in e.NewItems!.OfType<AccessSchedule>())
			{
				policy.AccessSchedules?.Add(item);
			}
		}

		if (e.Action == NotifyCollectionChangedAction.Remove)
		{
			foreach (var item in e.OldItems!.OfType<AccessSchedule>())
			{
				policy.AccessSchedules?.Remove(item);
			}
		}
	}

	[RelayCommand]
	private async Task PickAccessSchedule()
	{
		var result = await accessSchedulePicker.GetValue();

		if (result is null)
		{
			return;
		}

		AccessSchedules.Add(result);
	}

	[RelayCommand]
	private void DeleteSchedule(AccessSchedule schedule)
	{
		AccessSchedules.Remove(schedule);
	}

	private void BlockedTags_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		if (User?.Policy is not { } policy)
		{
			return;
		}

		if (e.Action == NotifyCollectionChangedAction.Add)
		{
			foreach (var item in e.NewItems!.OfType<string>())
			{
				if (policy.BlockedTags?.Contains(item) == false)
				{
					policy.BlockedTags?.Add(item);
				}
			}
		}

		if (e.Action == NotifyCollectionChangedAction.Remove)
		{
			foreach (var item in e.OldItems!.OfType<string>())
			{
				if (policy.BlockedTags?.Contains(item) == true)
				{
					policy.BlockedTags?.Remove(item);
				}
			}
		}
	}

	private void AllowedTags_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		if (User?.Policy is not { } policy)
		{
			return;
		}

		if (e.Action == NotifyCollectionChangedAction.Add)
		{
			foreach (var item in e.NewItems!.OfType<string>())
			{
				if (policy.AllowedTags?.Contains(item) == false)
				{
					policy.AllowedTags?.Add(item);
				}
			}
		}

		if (e.Action == NotifyCollectionChangedAction.Remove)
		{
			foreach (var item in e.OldItems!.OfType<string>())
			{
				if (policy.AllowedTags?.Contains(item) == true)
				{
					policy.AllowedTags?.Remove(item);
				}
			}
		}
	}
}

public partial class UnratedItemViewModel : ObservableObject
{
	public UnratedItemViewModel(UserDto user)
	{
		this.WhenAnyValue(x => x.IsSelected)
			.Subscribe(selected =>
			{
				if (selected && user.Policy?.BlockUnratedItems?.Contains(Item) == false)
				{
					user.Policy?.BlockUnratedItems?.Add(Item);
				}
				else if (!selected && user.Policy?.BlockUnratedItems?.Contains(Item) == true)
				{
					user.Policy?.BlockUnratedItems?.Remove(Item);
				}
			});
	}

	[ObservableProperty]
	public partial UnratedItem Item { get; set; }

	[ObservableProperty]
	public partial bool IsSelected { get; set; }
}
