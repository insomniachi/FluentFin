using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevWinUI;
using FluentFin.Contracts.ViewModels;
using FluentFin.Core.Contracts.Services;
using FlyleafLib.MediaPlayer;
using Jellyfin.Sdk.Generated.Models;
using System.Collections.ObjectModel;
using System.Web;

namespace FluentFin.ViewModels;

public partial class MediaSegmentsEditorViewModel(IJellyfinClient jellyfinClient) : ObservableObject, INavigationAware
{

	public BaseItemDto? Item { get; set; }

	public ObservableCollection<MediaSegmentViewModel> Segments { get; } = [];

	public Player MediaPlayer { get; } = new();

	public PlaylistViewModel Playlist { get; } = new();

	public long CurrentTimeTicks { get; set; }

	public MediaSegmentViewModel? PlayingSegment { get; set; }

	public Task OnNavigatedFrom()
	{
		MediaPlayer.Stop();
		return Task.CompletedTask;
	}

	public async Task OnNavigatedTo(object parameter)
	{
		if(parameter is not BaseItemDto dto)
		{
			return;
		}

		Item = dto;

		var response = await jellyfinClient.GetMediaUrl(Item);

		if(response is null)
		{
			return;
		}

		Playlist.Items.Add(new PlaylistItem
		{
			Dto = dto,
			Title = dto.Name ?? "",
			Media = response
		});

		Playlist.SelectedItem = Playlist.Items.First();

		var args = MediaPlayer.Open(HttpUtility.UrlDecode(response.Uri.ToString()));
		if (!args.Success)
		{
			return;
		}

		MediaPlayer.PropertyChanged += MediaPlayer_PropertyChanged;
		MediaPlayer.Pause();

		if(await jellyfinClient.GetMediaSegments(dto) is { Items.Count : > 0 } segmentsResults)
		{
			Segments.AddRange(segmentsResults.Items.Select(MediaSegmentViewModel.FromDto));
		}
	}

	private void MediaPlayer_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
	{
		if (e.PropertyName == nameof(MediaPlayer.CurTime))
		{
			CurrentTimeTicks = MediaPlayer.CurTime;

			if (PlayingSegment is { } segment && CurrentTimeTicks > segment.EndTicks)
			{
				MediaPlayer.Pause();
				PlayingSegment = null; 
			}
		}
	}

	[RelayCommand]
	private void AddSegment()
	{
		Segments.Add(new() { ItemId = Item?.Id });
	}

	[RelayCommand]
	private async Task DeleteSegment(MediaSegmentViewModel vm)
	{
		Segments.Remove(vm);

		if(vm.ItemId is { } id)
		{
			await jellyfinClient.DeleteMediaSegment(id);
		}
	}

	[RelayCommand]
	private async Task SubmitSegment(MediaSegmentViewModel vm)
	{
		if(Item is null)
		{
			return;
		}

		if(vm.ItemId is { } id)
		{
			await jellyfinClient.DeleteMediaSegment(id);
		}

		await jellyfinClient.CreateMediaSegment(Item, vm.ToDto());
	}

	[RelayCommand]
	private void PlaySegment(MediaSegmentViewModel vm)
	{
		PlayingSegment = vm;
		MediaPlayer.Play();
		MediaPlayer.CurTime = (long)vm.StartTicks;
	}
}

public partial class MediaSegmentViewModel : ObservableObject
{
	[ObservableProperty]
	public partial double StartTicks { get; set; }

	[ObservableProperty]
	public partial double EndTicks { get; set; }

	[ObservableProperty]
	public partial Guid? ItemId { get; set; }

	[ObservableProperty]
	public partial MediaSegmentDto_Type? Type { get; set; }

	public MediaSegmentDto ToDto() => new()
	{
		StartTicks = (long)StartTicks,
		EndTicks = (long)EndTicks,
		ItemId = ItemId ?? Guid.NewGuid(),
		Type = Type
	};

	public static MediaSegmentViewModel FromDto(MediaSegmentDto dto) => new()
	{
		StartTicks = dto.StartTicks ?? 0,
		EndTicks = dto.EndTicks ?? 0,
		ItemId = dto.ItemId,
		Type = dto.Type
	};
}
