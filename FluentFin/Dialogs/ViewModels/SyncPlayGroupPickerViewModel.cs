﻿using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentFin.Core;
using FluentFin.Core.Contracts.Services;
using Jellyfin.Sdk.Generated.Models;

namespace FluentFin.Dialogs.ViewModels;

public partial class SyncPlayGroupPickerViewModel(IJellyfinClient jellyfinClient) : ObservableObject, IHandleClose
{

	[ObservableProperty]
	public partial List<GroupInfoDto> Groups { get; set; }

	[ObservableProperty]
	public partial GroupInfoDto? SelectedGroup { get; set; }

	[ObservableProperty]
	public partial bool HasActiveGroups { get; set; }

	[ObservableProperty]
	public partial string SecondaryButtonText { get; set; }

	public bool CanClose { get; set; }


	public async Task Initialize()
	{
		Groups = await jellyfinClient.GetSyncPlayGroups();
		SelectedGroup = Groups.FirstOrDefault(x => x.GroupId == SessionInfo.GroupId);
		HasActiveGroups = Groups is { Count: > 0 };
		SecondaryButtonText = SelectedGroup is not null ? "Exit Group" : "Create Group";
	}

	[RelayCommand]
	private async Task JoinGroup(GroupInfoDto group)
	{
		if (group?.GroupId is not { } id)
		{
			return;
		}

		await jellyfinClient.JoinSyncPlayGroup(id);
		CanClose = true;
	}

	[RelayCommand]
	private async Task CreateOrExitGroup()
	{
		if (SelectedGroup is null)
		{
			await jellyfinClient.CreateSyncPlayGroup();
		}
		else
		{
			await jellyfinClient.LeaveSyncPlayGroup();
		}

	}

}
