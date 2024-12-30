using CommunityToolkit.Mvvm.ComponentModel;
using FluentFin.Contracts.ViewModels;
using FluentFin.Core.Contracts.Services;
using Jellyfin.Sdk.Generated.Models;
using System.Reflection;

namespace FluentFin.Core.ViewModels
{
	public partial class DashboardViewModel(IJellyfinClient jellyfinClient) : ObservableObject, INavigationAware
	{
		[ObservableProperty]
		public partial SystemInfo? SystemInfo { get; set; }

		[ObservableProperty]
		public partial List<SessionInfoDto> ActiveSessions { get; set; } = [];

		[ObservableProperty]
		public partial List<ActivityLogEntry> UserActivities { get; set; } = [];

		[ObservableProperty]
		public partial List<ActivityLogEntry> OtherActivities { get; set; } = [];

		[ObservableProperty]
		public partial List<NameValuePair> Paths { get; set; } = [];

		[ObservableProperty]
		public partial string Version { get; set; } = Assembly.GetEntryAssembly()!.GetName().Version!.ToString();


		public Task OnNavigatedFrom() => Task.CompletedTask;

		public async Task OnNavigatedTo(object parameter)
		{
			SystemInfo = await jellyfinClient.GetSystemInfo();
			ActiveSessions = await jellyfinClient.GetActiveSessions();
			
			Paths = [
				new NameValuePair { Name = "Cache", Value = SystemInfo?.CachePath },
				new NameValuePair { Name = "Logs", Value = SystemInfo?.LogPath },
				new NameValuePair { Name = "Metadata", Value = SystemInfo?.InternalMetadataPath },
				new NameValuePair { Name = "Transcodes", Value = SystemInfo?.TranscodingTempPath },
				new NameValuePair { Name = "Web", Value = SystemInfo?.WebPath },
			];
			
			var userActivityResult = await jellyfinClient.GetActivities(TimeProvider.System.GetUtcNow().AddDays(-1), true);
			if(userActivityResult is { Items.Count : > 0 })
			{
				UserActivities = userActivityResult.Items;
			}

			var otherActivityResult = await jellyfinClient.GetActivities(TimeProvider.System.GetUtcNow().AddDays(-7), false);
			if (otherActivityResult is { Items.Count: > 0 })
			{
				OtherActivities = otherActivityResult.Items;
			}
		}
	}
}
