using CommunityToolkit.Mvvm.Input;
using FluentFin.Core.Contracts.Services;
using FluentFin.Core.Services;
using FluentFin.Core.ViewModels;
using FluentFin.ViewModels;
using Jellyfin.Sdk.Generated.Models;

namespace FluentFin.Core;

public partial class GlobalCommands(INavigationServiceCore navigationService,
									IJellyfinClient jellyfinClient)
{
	[RelayCommand]
	public async Task PlayDto(BaseItemDto dto)
	{
		if (SessionInfo.GroupId is not null)
		{
			var ids = await GetItemIds(dto);
			var info = await jellyfinClient.GetItem(ids[0]!.Value);

			var request = new PlayRequestDto
			{
				PlayingItemPosition = 0,
				PlayingQueue = ids,
				StartPositionTicks = info?.UserData?.PlaybackPositionTicks ?? 0,
			};
			await jellyfinClient.SignalNewPlaylist(request);
		}
		else if (!string.IsNullOrEmpty(SessionInfo.RemoteSessionId) && SessionInfo.SessionId != SessionInfo.RemoteSessionId)
		{
			await jellyfinClient.PlayOnSession(SessionInfo.RemoteSessionId, await GetItemIds(dto));
		}
		else
		{
			navigationService.NavigateTo("FluentFin.ViewModels.VideoPlayerViewModel", dto);
		}
	}

	[RelayCommand]
	public async Task ToggleWatched(BaseItemDto dto)
	{
		await jellyfinClient.SetPlayed(dto, !(dto.UserData?.Played ?? false));
	}

	[RelayCommand]
	public async Task ToggleFavorite(BaseItemDto dto)
	{
		await jellyfinClient.SetIsFavorite(dto, !(dto.UserData?.IsFavorite ?? false));
	}

	[RelayCommand]
	public void DisplayDto(BaseItemDto dto)
	{
		switch (dto.Type)
		{
			case BaseItemDto_Type.Movie:
				navigationService.NavigateTo<MovieViewModel>(dto);
				break;
			case BaseItemDto_Type.Series:
				navigationService.NavigateTo<SeriesViewModel>(dto);
				break;
			case BaseItemDto_Type.Season:
				navigationService.NavigateTo<SeasonViewModel>(dto);
				break;
			case BaseItemDto_Type.Episode:
				navigationService.NavigateTo<EpisodeViewModel>(dto);
				break;
			default:
				break;
		}
	}

	[RelayCommand]
	public void NavigateToSegmentsEditor(BaseItemDto dto)
	{
		navigationService.NavigateTo("FluentFin.ViewModels.MediaSegmentsEditorViewModel", dto);
	}

	[RelayCommand]
	public async Task DeleteItem(BaseItemDto dto)
	{
		await jellyfinClient.DeleteItem(dto);
	}

	private async Task<List<Guid?>> GetItemIds(BaseItemDto dto)
	{
		var playlist = dto.Type switch
		{
			BaseItemDto_Type.Movie => PlaylistViewModel.FromMovie(dto),
			BaseItemDto_Type.Episode => await PlaylistViewModel.FromEpisode(jellyfinClient, dto),
			BaseItemDto_Type.Series => await PlaylistViewModel.FromSeries(jellyfinClient, dto),
			BaseItemDto_Type.Season => await PlaylistViewModel.FromSeason(jellyfinClient, dto),
			_ => new PlaylistViewModel()
		};

		playlist.AutoSelect();

		if (playlist.SelectedItem is null)
		{
			return [];
		}

		var index = playlist.Items.IndexOf(playlist.SelectedItem);
		return [.. playlist.Items.Skip(index).Select(x => x.Dto.Id)];
	}

}
