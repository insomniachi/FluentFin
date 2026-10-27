using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentFin.Contracts.ViewModels;
using FluentFin.Core.Contracts.Services;
using Jellyfin.Sdk.Generated.Models;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;

namespace FluentFin.Core.ViewModels;

public partial class DashboardViewModel(IJellyfinClient jellyfinClient,
										ITitleBarViewModel titleBarViewModel,
										[FromKeyedServices(UserInputs.MessageToSession)]IUserInput<string> stringUserInput,
										IContentDialogServiceCore contentDialogService) : ObservableObject, INavigationAware
{
	private string? _scanMediaLibraryTask;
	private IDisposable? _sessionsSubscription;

	[ObservableProperty]
	public partial SystemInfo? SystemInfo { get; set; }

	[ObservableProperty]
	public partial ObservableCollection<SessionInfoDtoViewModel> ActiveSessions { get; set; } = [];

	[ObservableProperty]
	public partial List<ActivityLogEntry> UserActivities { get; set; } = [];

	[ObservableProperty]
	public partial List<ActivityLogEntry> OtherActivities { get; set; } = [];

	[ObservableProperty]
	public partial List<NameValuePair> Paths { get; set; } = [];

	[ObservableProperty]
	public partial string Version { get; set; } = Assembly.GetEntryAssembly()!.GetName().Version!.ToString();


	public Task OnNavigatedFrom()
	{
		// disposing the SDK subscription sends SessionsStop
		_sessionsSubscription?.Dispose();
		_sessionsSubscription = null;
		return Task.CompletedTask;
	}

	public async Task OnNavigatedTo(object parameter)
	{
		SystemInfo = await jellyfinClient.GetSystemInfo();
		ActiveSessions = [..(await jellyfinClient.GetActiveSessions()).Where(x => x.Id != SessionInfo.SessionId).Select(CreateViewModel)];

		Paths = [
			new NameValuePair { Name = "Cache", Value = SystemInfo?.CachePath },
			new NameValuePair { Name = "Logs", Value = SystemInfo?.LogPath },
			new NameValuePair { Name = "Metadata", Value = SystemInfo?.InternalMetadataPath },
			new NameValuePair { Name = "Transcodes", Value = SystemInfo?.TranscodingTempPath },
			new NameValuePair { Name = "Web", Value = SystemInfo?.WebPath },
		];

		var userActivityResult = await jellyfinClient.GetActivities(TimeProvider.System.GetUtcNow().AddDays(-1), true);
		if (userActivityResult is { Items.Count: > 0 })
		{
			UserActivities = userActivityResult.Items;
		}

		var otherActivityResult = await jellyfinClient.GetActivities(TimeProvider.System.GetUtcNow().AddDays(-7), false);
		if (otherActivityResult is { Items.Count: > 0 })
		{
			OtherActivities = otherActivityResult.Items;
		}

		var tasks = await jellyfinClient.GetScheduledTasks(true);

		_scanMediaLibraryTask = tasks.FirstOrDefault(x => x.Name == "Scan Media Library")?.Id;

		_sessionsSubscription?.Dispose();
		_sessionsSubscription = jellyfinClient.ObserveWebSocket<SessionsMessage>()
			.ObserveOn(RxSchedulers.MainThreadScheduler)
			.Select(x => x.Data?.Where(x => x.Id != SessionInfo.SessionId) ?? [])
			.Subscribe(sessions =>
			{
				foreach (var dto in sessions)
				{
					if(dto.LastActivityDate is { } date && (TimeProvider.System.GetUtcNow() - date).TotalMinutes > 30)
					{
						continue;
					}

					if(ActiveSessions.FirstOrDefault(x => x.Id == dto.Id) is { } vm)
					{
						vm.Update(dto);
					}
					else
					{
						ActiveSessions.Add(CreateViewModel(dto));
					}
				}
			});
	}

	[RelayCommand]
	private async Task Shutdown()
	{
		await titleBarViewModel.Logout();
		await jellyfinClient.Shutdown();
	}

	[RelayCommand]
	private async Task Restart()
	{
		await titleBarViewModel.Logout();
		await jellyfinClient.Restart();
	}

	[RelayCommand]
	private async Task StartScan()
	{
		if (string.IsNullOrEmpty(_scanMediaLibraryTask))
		{
			return;
		}

		await jellyfinClient.RunTask(_scanMediaLibraryTask);
	}

	private SessionInfoDtoViewModel CreateViewModel(SessionInfoDto dto)
	{
		return new SessionInfoDtoViewModel(dto, jellyfinClient, contentDialogService, stringUserInput);
	}
}
