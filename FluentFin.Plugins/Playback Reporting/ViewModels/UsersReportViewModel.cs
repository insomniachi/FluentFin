using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentFin.Contracts.ViewModels;
using ReactiveUI;

namespace FluentFin.Plugins.Playback_Reporting.ViewModels;

public partial class UsersReportViewModel : ObservableObject, INavigationAware
{
	[ObservableProperty]
	public partial List<UserActivity> Activities { get; set; } = [];

	[ObservableProperty]
	public partial DateTimeOffset EndDate { get; set; } = TimeProvider.System.GetLocalNow();

	[ObservableProperty]
	public partial int NumberOfWeeks { get; set; } = 4;

	public IEnumerable<int> Weeks { get; } = Enumerable.Range(1, 11);

	public Task OnNavigatedFrom() => Task.CompletedTask;

	public Task OnNavigatedTo(object parameter)
	{
		this.WhenAnyValue(x => x.EndDate, x => x.NumberOfWeeks)
			.Select(x => new { EndDate = x.Item1, Days = x.Item2 * 7 })
			.SelectMany(x => PlaybackReportingHelper.GetUserActivity(x.Days, x.EndDate))
			.ObserveOn(RxApp.MainThreadScheduler)
			.Subscribe(activity => Activities = activity);

		return Task.CompletedTask;
	}
}
