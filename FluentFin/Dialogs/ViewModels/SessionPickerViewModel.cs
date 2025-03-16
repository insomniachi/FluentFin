using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentFin.Core.Contracts.Services;
using FluentFin.ViewModels;
using Jellyfin.Sdk.Generated.Models;

namespace FluentFin.Dialogs.ViewModels;

public partial class SessionPickerViewModel(IJellyfinClient jellyfinClient) : ObservableObject, IHandleClose
{
    private IEnumerable<Guid?> _itemIds = [];

    [ObservableProperty]
    public partial List<SessionInfoDto> Sessions { get; set; } = [];

    public bool CanClose { get; set; }

    public async Task Initialize(BaseItemDto dto)
    {
        Sessions = await jellyfinClient.GetControllableSessions();
        _itemIds = await GetItemIds(dto);
    }

    [RelayCommand]
    private async Task PlayOnSession(SessionInfoDto session)
    {
        if(string.IsNullOrEmpty(session.Id))
        {
            return;
        }

        await jellyfinClient.PlayOnSession(session.Id, _itemIds);
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

        if(playlist.SelectedItem is null)
        {
            return [];
        }

        var index = playlist.Items.IndexOf(playlist.SelectedItem);
        return [.. playlist.Items.Skip(index).Select(x => x.Dto.Id)];
    }
}
