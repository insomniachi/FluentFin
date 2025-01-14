using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using FluentFin.Contracts.ViewModels;
using FluentFin.Core.Contracts.Services;
using FlyleafLib.MediaPlayer;
using Jellyfin.Sdk.Generated.Models;
using System.Collections.ObjectModel;
using System.Web;

namespace FluentFin.ViewModels;

public partial class MediaSegmentsEditorViewModel(IJellyfinClient jellyfinClient) : ObservableObject, INavigationAware
{

	[ObservableProperty]
	public partial PlaylistViewModel Playlist { get; set; } = new();
	public ObservableCollection<MediaSegmentViewModel> Segments { get; } = [];
	public Player MediaPlayer { get; } = new();
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
		
		MediaPlayer.PropertyChanged += MediaPlayer_PropertyChanged;


		Playlist = dto.Type switch
		{
			BaseItemDto_Type.Movie => PlaylistViewModel.FromMovie(dto),
			BaseItemDto_Type.Episode => await PlaylistViewModel.FromEpisode(jellyfinClient, dto),
			BaseItemDto_Type.Series => await PlaylistViewModel.FromSeries(jellyfinClient, dto),
			BaseItemDto_Type.Season => await PlaylistViewModel.FromSeason(jellyfinClient, dto),
			_ => new PlaylistViewModel()
		};

		if (Playlist.Items.Count == 0)
		{
			return;
		}

		Playlist.PropertyChanged += OnPlaylistPropertyChanged;

		if(dto.Type == BaseItemDto_Type.Episode)
		{
			Playlist.SelectedItem = Playlist.Items.FirstOrDefault(x => x.Dto.IndexNumber == dto.IndexNumber && x.Dto.ParentIndexNumber == dto.ParentIndexNumber);
		}
		else
		{
			Playlist.SelectedItem = Playlist.Items.FirstOrDefault();
		}
	}

	private async void OnPlaylistPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
	{
		if (e.PropertyName != nameof(Playlist.SelectedItem))
		{
			return;
		}

		if (Playlist.SelectedItem is not { } selectedItem)
		{
			return;
		}

		MediaPlayer.Stop();

		var full = await jellyfinClient.GetItem(selectedItem.Dto.Id ?? Guid.Empty);

		if (full is null)
		{
			return;
		}

		var mediaResponse = await jellyfinClient.GetMediaUrl(full);

		if (mediaResponse is null)
		{
			return;
		}

		selectedItem.Media = mediaResponse;

		var args = MediaPlayer.Open(HttpUtility.UrlDecode(mediaResponse.Uri.ToString()));
		if (!args.Success)
		{
			return;
		}

		MediaPlayer.Pause();

		if (await jellyfinClient.GetMediaSegments(selectedItem.Dto) is { Items.Count: > 0 } segmentsResults)
		{
			Segments.Clear();
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
		Segments.Add(new() { ItemId = Playlist.SelectedItem?.Dto?.Id });
	}

	[RelayCommand]
	private async Task DeleteSegment(MediaSegmentViewModel vm)
	{
		Segments.Remove(vm);

		if(vm.Id is { } id)
		{
			await jellyfinClient.DeleteMediaSegment(id);
		}
	}

	[RelayCommand]
	private async Task SubmitSegment(MediaSegmentViewModel vm)
	{
		if(vm.Id is { } id)
		{
			await jellyfinClient.DeleteMediaSegment(id);
		}

		await jellyfinClient.CreateMediaSegment(vm.ToDto());
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
	public partial long StartTicks { get; set; }

	[ObservableProperty]
	public partial long EndTicks { get; set; }

	[ObservableProperty]
	public partial Guid? ItemId { get; set; }

	[ObservableProperty]
	public partial MediaSegmentDto_Type? Type { get; set; }

	public Guid? Id { get; set; }

	public MediaSegmentDto ToDto() => new()
	{
		StartTicks = StartTicks,
		EndTicks = EndTicks,
		ItemId = ItemId ?? Guid.NewGuid(),
		Type = Type,
		Id = Id ?? Guid.NewGuid()
	};

	public static MediaSegmentViewModel FromDto(MediaSegmentDto dto) => new()
	{
		StartTicks = dto.StartTicks ?? 0,
		EndTicks = dto.EndTicks ?? 0,
		ItemId = dto.ItemId,
		Type = dto.Type,
		Id = dto.Id
	};
}
