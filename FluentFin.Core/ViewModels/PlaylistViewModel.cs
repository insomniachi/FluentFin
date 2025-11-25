using System.Collections.ObjectModel;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentFin.Core.Contracts.Services;
using FluentFin.Core.WebSockets.Messages;
using Jellyfin.Sdk.Generated.Models;
using ReactiveUI;

namespace FluentFin.ViewModels;

public partial class PlaylistViewModel : ObservableObject
{
	public ObservableCollection<PlaylistItem> Items { get; } = [];

	[ObservableProperty]
	public partial PlaylistItem? SelectedItem { get; set; }

	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(SelectNextCommand))]
	public partial bool CanSelectNext { get; set; }

	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(SelectPrevCommand))]
	public partial bool CanSelectPrev { get; set; }

	public PlaylistViewModel()
	{
		this.WhenAnyValue(x => x.SelectedItem)
			.Select(selectedItem =>
			{
				if (selectedItem is null)
				{
					return false;
				}

				return Items.IndexOf(selectedItem) < Items.Count - 1;
			})
			.Subscribe(value => CanSelectNext = value);

		this.WhenAnyValue(x => x.SelectedItem)
			.Select(selectedItem =>
			{
				if (selectedItem is null)
				{
					return false;
				}

				return Items.IndexOf(selectedItem) > 0;
			})
			.Subscribe(value => CanSelectPrev = value);
	}

	public void AutoSelect()
	{
		if (SelectedItem is not null)
		{
			return;
		}

		if (Items.Count == 0)
		{
			return;
		}

		SelectedItem = Items.FirstOrDefault(x => x.Dto.UserData?.Played is false or null) ?? Items.First();
	}


	[RelayCommand(CanExecute = nameof(CanSelectNext))]
	public void SelectNext()
	{
		if (SelectedItem is null)
		{
			return;
		}

		RxApp.MainThreadScheduler.Schedule(() =>
		{
			SelectedItem = Items[Items.IndexOf(SelectedItem) + 1];
		});
	}

	[RelayCommand(CanExecute = nameof(CanSelectPrev))]
	public void SelectPrev()
	{
		if (SelectedItem is null)
		{
			return;
		}

		RxApp.MainThreadScheduler.Schedule(() =>
		{
			SelectedItem = Items[Items.IndexOf(SelectedItem) - 1];
		});
	}

	public static PlaylistViewModel FromMovie(BaseItemDto dto)
	{
		return new PlaylistViewModel
		{
			Items =
			{
				new PlaylistItem
				{
					Title = dto.Name ?? "",
					Dto = dto
				}
			}
		};
	}

	public static async Task<PlaylistViewModel> FromSeries(IJellyfinClient jellyfinClient, BaseItemDto series)
	{
		var playlist = new PlaylistViewModel();

		var seasons = await jellyfinClient.GetItems(series);

		if (seasons is null or { Items: null })
		{
			return playlist;
		}

		foreach (var season in seasons.Items)
		{
			await FromSeason(jellyfinClient, season, playlist);
		}

		return playlist;
	}

	public static PlaylistViewModel FromSyncPlay(IReadOnlyList<SyncPlayQueueItem> items)
	{
		var playlist = new PlaylistViewModel();
		foreach (var item in items)
		{
			playlist.Items.Add(new PlaylistItem
			{
				Title = item.ItemId.ToString(),
				Dto = new BaseItemDto
				{
					Id = item.ItemId
				}
			});
		}
		return playlist;
	}

	public static async Task<PlaylistViewModel> FromEpisode(IJellyfinClient jellyfinClient, BaseItemDto episode)
	{
		var playlist = new PlaylistViewModel();

		if (episode.SeriesId is not { } seriesId)
		{
			return playlist;
		}

		var series = await jellyfinClient.GetItem(seriesId);

		if (series is null)
		{
			return playlist;
		}

		return await FromSeries(jellyfinClient, series);
	}

	public static async Task<PlaylistViewModel> FromSeason(IJellyfinClient jellyfinClient, BaseItemDto season)
	{
		var playlist = new PlaylistViewModel();

		if (season.SeriesId is not { } seriesId)
		{
			return playlist;
		}

		var series = await jellyfinClient.GetItem(seriesId);

		if (series is null)
		{
			return playlist;
		}

		return await FromSeries(jellyfinClient, series);
	}

	public static async Task FromSeason(IJellyfinClient jellyfinClient, BaseItemDto season, PlaylistViewModel playlist)
	{
		var episodes = await jellyfinClient.GetItems(season);

		if (episodes is null or { Items: null })
		{
			return;
		}

		foreach (var episode in episodes.Items)
		{
			playlist.Items.Add(new PlaylistItem
			{
				Title = episode.Name ?? "",
				Dto = episode
			});
		}
	}
}

public partial class PlaylistItem : ObservableObject
{
	required public string Title { get; set; }
	required public BaseItemDto Dto { get; set; }

	[ObservableProperty]
	public partial MediaResponse? Media { get; set; }
}
