﻿using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevWinUI;
using FluentFin.Core.Contracts.Services;
using Jellyfin.Sdk.Generated.Models;

namespace FluentFin.Dialogs.ViewModels;

public partial class EditSubtitlesViewModel(IJellyfinClient jellyfinClient) : ObservableObject, IHandleClose, IBaseItemDialogViewModel
{
	public bool CanClose { get; set; }

	[ObservableProperty]
	public partial BaseItemDto? Item { get; set; }

	[ObservableProperty]
	public partial CultureInfo? SelectedCulture { get; set; }

	[ObservableProperty]
	public partial List<RemoteSubtitleInfo> RemoteSubtitles { get; set; } = [];

	public ObservableCollection<MediaStream> Subtitles { get; } = [];

	public List<CultureInfo> Cultures { get; } = [.. CultureInfo.GetCultures(CultureTypes.NeutralCultures)];

	public async Task Initialize(BaseItemDto item)
	{
		if (item.Id is not { } id)
		{
			return;
		}

		Item = await jellyfinClient.GetItem(id);

		if (Item is null)
		{
			return;
		}

		Subtitles.AddRange(Item.MediaStreams?.Where(x => x.Type == MediaStream_Type.Subtitle).ToList() ?? []);
		SelectedCulture = Cultures.FirstOrDefault(x => x.ThreeLetterISOLanguageName.Equals("eng", StringComparison.OrdinalIgnoreCase));
	}

	[RelayCommand]
	public async Task SearchSubtitles()
	{
		if (Item is null || SelectedCulture is null)
		{
			return;
		}

		RemoteSubtitles = await jellyfinClient.SearchSubtitles(Item, SelectedCulture);
	}

	[RelayCommand]
	public async Task DownloadSubtitle(RemoteSubtitleInfo info)
	{
		if (Item is null)
		{
			return;
		}

		await jellyfinClient.DownloadSubtitle(Item, info);
	}

	[RelayCommand]
	public async Task DeleteSubtitle(MediaStream info)
	{
		if (Item is null)
		{
			return;
		}

		if (info.IsExternal == false)
		{
			return;
		}

		Subtitles.Remove(info);

		await jellyfinClient.DeleteSubtitle(Item, info);
	}
}
