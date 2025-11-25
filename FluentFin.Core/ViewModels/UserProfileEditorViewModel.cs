using System.Reactive.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentFin.Contracts.ViewModels;
using FluentFin.Core.Contracts.Services;
using Jellyfin.Sdk.Generated.Models;
using ReactiveUI;

namespace FluentFin.Core.ViewModels;

public partial class UserSectionEditorViewModel : ObservableObject, INavigationAware
{
	[ObservableProperty]
	public partial UserDto User { get; set; }

	public Task OnNavigatedFrom() => Task.CompletedTask;

	public Task OnNavigatedTo(object parameter)
	{
		if (parameter is not UserDto user)
		{
			return Task.CompletedTask;
		}

		User = user;

		Initialize(user);

		return Task.CompletedTask;
	}

	protected virtual Task Initialize(UserDto user) { return Task.CompletedTask; }
}

public partial class UserProfileEditorViewModel(IJellyfinClient jellyfinClient) : UserSectionEditorViewModel
{
	[ObservableProperty]
	public partial string? Name { get; set; }

	[ObservableProperty]
	public partial bool AllowRemoteConnections { get; set; }

	[ObservableProperty]
	public partial bool AllowServerManagement { get; set; }

	[ObservableProperty]
	public partial bool AllowCollectionManagement { get; set; }

	[ObservableProperty]
	public partial bool AllowSubtitleEdits { get; set; }

	[ObservableProperty]
	public partial bool AllowLiveTvAccess { get; set; }

	[ObservableProperty]
	public partial bool AllowLiveTvRecordingManagement { get; set; }

	[ObservableProperty]
	public partial bool AllowMediaPlayback { get; set; }

	[ObservableProperty]
	public partial bool AllowAudioPlaybackTranscoding { get; set; }

	[ObservableProperty]
	public partial bool AllowVideoPlaybackTranscoding { get; set; }

	[ObservableProperty]
	public partial bool AllowPlaybackWithoutReEncoding { get; set; }

	[ObservableProperty]
	public partial bool ForceTranscoding { get; set; }

	[ObservableProperty]
	public partial double InternetStreamingBitrateLimit { get; set; }

	[ObservableProperty]
	public partial UserPolicy_SyncPlayAccess SyncPlayAccess { get; set; }

	[ObservableProperty]
	public partial bool AllowMediaDeletionFromAllLibraries { get; set; }

	[ObservableProperty]
	public partial bool AllowRemoteControlOfOtherUsers { get; set; }

	[ObservableProperty]
	public partial bool AllowSharedDeviceControl { get; set; }

	[ObservableProperty]
	public partial bool AllowMediaDownloads { get; set; }

	[ObservableProperty]
	public partial bool IsDisabled { get; set; }

	[ObservableProperty]
	public partial bool HideFromLogin { get; set; }

	[ObservableProperty]
	public partial double FailedLoginTriesBeforeLockout { get; set; } = -1;

	[ObservableProperty]
	public partial double MaximumNumberOfSimultaneousSessions { get; set; }

	[ObservableProperty]
	public partial List<BaseItemDto> AllowContentDeletionFromFolders { get; set; } = [];

	protected override async Task Initialize(UserDto user)
	{
		if (user.Policy is not { } policy)
		{
			return;
		}

		var folders = await jellyfinClient.GetMediaFolders();

		Name = user.Name;
		AllowRemoteConnections = policy.EnableRemoteAccess ?? false;
		AllowServerManagement = policy.IsAdministrator ?? false;
		AllowCollectionManagement = policy.EnableCollectionManagement ?? false;
		AllowSubtitleEdits = policy.EnableSubtitleManagement ?? false;
		AllowLiveTvAccess = policy.EnableLiveTvAccess ?? false;
		AllowLiveTvRecordingManagement = policy.EnableLiveTvManagement ?? false;
		AllowMediaPlayback = policy.EnableMediaPlayback ?? false;
		AllowAudioPlaybackTranscoding = policy.EnableAudioPlaybackTranscoding ?? false;
		AllowVideoPlaybackTranscoding = policy.EnableVideoPlaybackTranscoding ?? false;
		AllowPlaybackWithoutReEncoding = policy.EnablePlaybackRemuxing ?? false;
		ForceTranscoding = policy.ForceRemoteSourceTranscoding ?? false;
		InternetStreamingBitrateLimit = policy.RemoteClientBitrateLimit ?? 0;
		SyncPlayAccess = policy.SyncPlayAccess ?? UserPolicy_SyncPlayAccess.None;
		AllowMediaDeletionFromAllLibraries = policy.EnableContentDeletion ?? false;
		IsDisabled = policy.IsDisabled ?? false;
		AllowMediaDownloads = policy.EnableContentDownloading ?? false;
		HideFromLogin = policy.IsHidden ?? false;


		if (folders is { Items.Count: > 0 } && policy.EnableContentDeletionFromFolders is { Count: > 0 })
		{
			AllowContentDeletionFromFolders = policy.EnableContentDeletionFromFolders.Select(x => folders.Items.First(folder => folder.Id == Guid.Parse(x))).ToList() ?? [];
		}

		this.WhenAnyValue(x => x.AllowRemoteConnections).Subscribe(value => policy.EnableRemoteAccess = value);
		this.WhenAnyValue(x => x.AllowServerManagement).Subscribe(value => policy.IsAdministrator = value);
		this.WhenAnyValue(x => x.AllowCollectionManagement).Subscribe(value => policy.EnableCollectionManagement = value);
		this.WhenAnyValue(x => x.AllowSubtitleEdits).Subscribe(value => policy.EnableSubtitleManagement = value);
		this.WhenAnyValue(x => x.AllowLiveTvAccess).Subscribe(value => policy.EnableLiveTvAccess = value);
		this.WhenAnyValue(x => x.AllowLiveTvRecordingManagement).Subscribe(value => policy.EnableLiveTvManagement = value);
		this.WhenAnyValue(x => x.AllowMediaPlayback).Subscribe(value => policy.EnableMediaPlayback = value);
		this.WhenAnyValue(x => x.AllowAudioPlaybackTranscoding).Subscribe(value => policy.EnableAudioPlaybackTranscoding = value);
		this.WhenAnyValue(x => x.AllowVideoPlaybackTranscoding).Subscribe(value => policy.EnableVideoPlaybackTranscoding = value);
		this.WhenAnyValue(x => x.AllowPlaybackWithoutReEncoding).Subscribe(value => policy.EnablePlaybackRemuxing = value);
		this.WhenAnyValue(x => x.ForceTranscoding).Subscribe(value => policy.ForceRemoteSourceTranscoding = value);
		this.WhenAnyValue(x => x.InternetStreamingBitrateLimit).Subscribe(value => policy.RemoteClientBitrateLimit = (int?)value);
		this.WhenAnyValue(x => x.SyncPlayAccess).Subscribe(value => policy.SyncPlayAccess = value);
		this.WhenAnyValue(x => x.AllowMediaDeletionFromAllLibraries).Subscribe(value => policy.EnableContentDeletion = value);
		this.WhenAnyValue(x => x.IsDisabled).Subscribe(value => policy.IsDisabled = value);
		this.WhenAnyValue(x => x.AllowMediaDownloads).Subscribe(value => policy.EnableContentDownloading = value);
		this.WhenAnyValue(x => x.HideFromLogin).Subscribe(value => policy.IsHidden = value);
	}
}