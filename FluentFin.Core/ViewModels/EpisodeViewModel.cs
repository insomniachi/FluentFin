using CommunityToolkit.Mvvm.ComponentModel;
using FluentFin.Contracts.ViewModels;
using FluentFin.Core.Contracts.Services;
using Jellyfin.Sdk.Generated.Models;

namespace FluentFin.Core.ViewModels;

public partial class EpisodeViewModel(IJellyfinClient jellyfinClient) : ObservableObject, INavigationAware
{
	[ObservableProperty]
	public partial BaseItemDto Dto { get; set; }

	[ObservableProperty]
	public partial IList<MediaStream> AudioStreams { get; set; }

	[ObservableProperty]
	public partial MediaStream? SelectedAudio { get; set; }

	[ObservableProperty]
	public partial IList<MediaStream> SubtitleStreams { get; set; }

	[ObservableProperty]
	public partial MediaStream? SelectedSubtitle { get; set; }

	[ObservableProperty]
	public partial IEnumerable<BaseItemPerson> GuestStars { get; set; }

	[ObservableProperty]
	public partial IEnumerable<BaseItemPerson> CastAndCrew { get; set; }

	[ObservableProperty]
	public partial string VideoTitle { get; set; }

	[ObservableProperty]
	public partial bool HasSubtitles { get; set; }

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

		await UpdateEpisode(id);
	}

	private async Task UpdateEpisode(Guid id)
	{
		var movie = await jellyfinClient.GetItem(id);
		if (movie is null)
		{
			return;
		}
		Dto = movie;

		if(Dto.People is { Count: > 0 } people)
		{
			CastAndCrew = people.Where(x => x.Type != BaseItemPerson_Type.GuestStar);
			GuestStars = people.Where(x => x.Type == BaseItemPerson_Type.GuestStar);
		}

		if (Dto.MediaStreams is { Count: > 0 } streams)
		{
			VideoTitle = streams.FirstOrDefault(x => x.Type == MediaStream_Type.Video)?.DisplayTitle ?? string.Empty;

			AudioStreams = streams.Where(x => x.Type == MediaStream_Type.Audio).ToList();
			SelectedAudio = AudioStreams.FirstOrDefault(x => x.IsDefault == true) ?? AudioStreams.FirstOrDefault();

			var defaultItem = new MediaStream();
			typeof(MediaStream).GetProperty(nameof(MediaStream.DisplayTitle))!.SetValue(defaultItem, "None");

			SubtitleStreams = [defaultItem, .. streams.Where(x => x.Type == MediaStream_Type.Subtitle).ToList()];
			SelectedSubtitle = SubtitleStreams.FirstOrDefault(x => x.IsDefault == true) ?? SubtitleStreams.FirstOrDefault();
			HasSubtitles = SubtitleStreams.Count > 1;
		}
	}
}
