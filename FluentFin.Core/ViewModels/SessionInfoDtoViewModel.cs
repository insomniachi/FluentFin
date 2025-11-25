using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentFin.Core.Contracts.Services;
using Jellyfin.Sdk.Generated.Models;

namespace FluentFin.Core.ViewModels;

public partial class SessionInfoDtoViewModel(SessionInfoDto dto,
											 IJellyfinClient jellyfinClient,
											 IContentDialogServiceCore contentDialogService,
											 IUserInput<string> stringUserInput) : ObservableObject
{
	public string? Id { get; } = dto.Id;
	public string? Client { get; } = dto.Client;
	public string? ApplicationVersion { get; } = dto.ApplicationVersion;
	public string? DeviceName { get; } = dto.DeviceName;
	public string? UserName { get; set; } = dto.UserName;
	public bool CanRecieveMessage { get; set; } = dto.Capabilities?.SupportedCommands?.Contains(GeneralCommandType.DisplayMessage) == true;

	[ObservableProperty]
	public partial string? IconUrl { get; set; } = dto.Capabilities?.IconUrl;

	[ObservableProperty]
	public partial BaseItemDto? NowPlayingItem { get; set; } = dto.NowPlayingItem;

	[ObservableProperty]
	public partial PlayerStateInfo? PlayState { get; set; } = dto.PlayState;

	[ObservableProperty]
	public partial DateTimeOffset? LastActivityDate { get; set; } = dto.LastActivityDate;

	[ObservableProperty]
	public partial bool MediaControlsVisible { get; set; } = dto.Capabilities?.SupportsMediaControl == true && dto.NowPlayingItem is not null;

	[RelayCommand]
	private async Task SendMessage()
	{
		if(string.IsNullOrEmpty(Id))
		{
			return;
		}	

		var text = await stringUserInput.GetValue();
		if(string.IsNullOrEmpty(text))
		{
			return;
		}

		await jellyfinClient.SendMessage(Id, new MessageCommand
		{
			Header = @"Message from admin",
			Text = text,
			TimeoutMs = 5000
		});
	}

	[RelayCommand]
	private async Task TogglePlayPause()
	{
		if (string.IsNullOrEmpty(Id))
		{
			return;
		}

		await jellyfinClient.TogglePlayPause(Id);
	}

	[RelayCommand]
	private async Task Stop()
	{
		if (string.IsNullOrEmpty(Id))
		{
			return;
		}

		await jellyfinClient.Stop(Id);
	}

	[RelayCommand]
	private async Task Info()
	{
		switch (PlayState?.PlayMethod)
		{
			case PlayerStateInfo_PlayMethod.DirectPlay:
				await contentDialogService.ShowMessage("Direct Playing",
													   "The source file is entirely compatible with this client and the session is receiving the file without modifications.");
				break;
			case PlayerStateInfo_PlayMethod.Transcode:
				await contentDialogService.ShowMessage("Transcoding", "The source file is not compatible with this client and the session is receiving a modified version of the file.");
				break;
		}
	}

	public void Update(SessionInfoDto update)
	{
		LastActivityDate = update.LastActivityDate;
		PlayState = update.PlayState;
		
		if(NowPlayingItem is null)
		{
			NowPlayingItem = update.NowPlayingItem;
		}
		else if(!NowPlayingItem.Id.Equals(update.NowPlayingItem?.Id))
		{
			NowPlayingItem = update.NowPlayingItem;
		}

		MediaControlsVisible = dto.Capabilities?.SupportsMediaControl == true && NowPlayingItem is not null;
	}
}
