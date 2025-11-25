using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentFin.Contracts.ViewModels;
using FluentFin.Core.Contracts.Services;
using Jellyfin.Sdk.Generated.Models;

namespace FluentFin.Core.ViewModels;

public partial class MovieViewModel(IJellyfinClient jellyfinClient) : ObservableObject, INavigationAware
{
	[ObservableProperty]
	public partial BaseItemDto Dto { get; set; }

	[ObservableProperty]
	public partial ObservableCollection<BaseItemViewModel> Similar { get; set; }

	[ObservableProperty]
	public partial IList<MediaStream> AudioStreams { get; set; }

	[ObservableProperty]
	public partial MediaStream? SelectedAudio { get; set; }

	[ObservableProperty]
	public partial IList<MediaStream> SubtitleStreams { get; set; }

	[ObservableProperty]
	public partial MediaStream? SelectedSubtitle { get; set; }

	[ObservableProperty]
	public partial bool HasSubtitles { get; set; }

	[ObservableProperty]
	public partial string VideoTitle { get; set; }

	public IJellyfinClient JellyfinClient { get; } = jellyfinClient;

	public Task OnNavigatedFrom() => Task.CompletedTask;

	public async Task OnNavigatedTo(object parameter)
	{
		if (parameter is not BaseItemDto dto)
		{
			return;
		}

		if (dto.Id is not { } id)
		{
			return;
		}

		await UpdateMovie(id);
		await UpdateSimilar(dto);
	}

	private async Task UpdateMovie(Guid id)
	{
		var movie = await JellyfinClient.GetItem(id);
		if (movie is null)
		{
			return;
		}

		Dto = movie;

		if (Dto.MediaStreams is { Count: > 0 } streams)
		{
			VideoTitle = streams.FirstOrDefault(x => x.Type == MediaStream_Type.Video)?.DisplayTitle ?? string.Empty;

			AudioStreams = streams.Where(x => x.Type == MediaStream_Type.Audio).ToList();
			SelectedAudio = AudioStreams.FirstOrDefault(x => x.IsDefault == true) ?? AudioStreams.FirstOrDefault();

			var defaultItem = new MediaStream();
			typeof(MediaStream).GetProperty(nameof(MediaStream.DisplayTitle))!.SetValue(defaultItem, "None");

			SubtitleStreams = [defaultItem, .. streams.Where(x => x.Type == MediaStream_Type.Subtitle).ToList()];
			HasSubtitles = SubtitleStreams.Count > 1;
		}

		if (Dto.MediaSources is { Count: 1 } sources)
		{
			if (sources[0].DefaultSubtitleStreamIndex > 0)
			{
				SelectedSubtitle = SubtitleStreams.FirstOrDefault(x => x.Index == sources[0].DefaultSubtitleStreamIndex);
			}
			else
			{
				SelectedSubtitle = SubtitleStreams?.FirstOrDefault();
			}
		}
	}

	private async Task UpdateSimilar(BaseItemDto dto)
	{
		var response = await JellyfinClient.GetSimilarItems(dto);

		if (response is null or { Items: null })
		{
			return;
		}

		Similar = [.. response.Items.Select(BaseItemViewModel.FromDto)];
	}
}
