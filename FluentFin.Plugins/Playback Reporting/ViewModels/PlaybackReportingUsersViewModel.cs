using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentFin.Contracts.ViewModels;

namespace FluentFin.Plugins.Playback_Reporting.ViewModels;

public partial class PlaybackReportingUsersViewModel : ObservableObject, INavigationAware
{
	[ObservableProperty]
	public partial List<UserActivity> Activities { get; set; } = [];

	public Task OnNavigatedFrom() => Task.CompletedTask;

	public async Task OnNavigatedTo(object parameter)
	{
		Activities = await PlaybackReportingHelper.GetUserActivity(28, TimeProvider.System.GetUtcNow());
	}
}
