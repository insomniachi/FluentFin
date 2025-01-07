using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentFin.Core.Contracts.Services;
using Jellyfin.Sdk.Generated.Models;
using ReactiveUI;
using System.Reactive.Linq;

namespace FluentFin.ViewModels;

public partial class PlaylistViewModel : ObservableObject
{
	public List<PlaylistItem> Items { get; } = [];

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

				return Items.Any(pi => pi.Dto.IndexNumber > selectedItem.Dto.IndexNumber);
			})
			.Subscribe(value => CanSelectNext = value);

		this.WhenAnyValue(x => x.SelectedItem)
			.Select(selectedItem =>
			{
				if (selectedItem is null)
				{
					return false;
				}

				return Items.Any(pi => pi.Dto.IndexNumber < selectedItem.Dto.IndexNumber);
			})
			.Subscribe(value => CanSelectPrev = value);
	}

	public void AutoSelect()
	{
		if(SelectedItem is not null)
		{
			return;
		}

		if(Items.Count == 0)
		{
			return;
		}

		SelectedItem = Items.FirstOrDefault(x => x.Dto.UserData?.Played is false or null) ?? Items.First();
	}


	[RelayCommand(CanExecute = nameof(CanSelectNext))]
	public void SelectNext()
	{
		SelectedItem = Items.First(x => x.Dto.IndexNumber > SelectedItem?.Dto.IndexNumber);
	}

	[RelayCommand(CanExecute = nameof(CanSelectPrev))]
	public void SelectPrev()
	{
		SelectedItem = Items.First(x => x.Dto.IndexNumber < SelectedItem?.Dto.IndexNumber);
	}
}

public class PlaylistItem
{
	required public string Title { get; set; }
	required public BaseItemDto Dto { get; set; }
	public MediaResponse? Media { get; set; }
}
