using FluentFin.Core;
using FluentFin.Core.Services;
using FluentFin.Plugins.Playback_Reporting.ViewModels;
using FluentFin.Plugins.Playback_Reporting.Views;
using FluentFin.UI.Core.Contracts.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FluentFin.Plugins.Playback_Reporting;

public class PlaybackReportingPlugin : IPlugin
{
	public void AddNavigationViewItem(INavigationViewServiceCore navigationViewService)
	{
		if(!SessionInfo.HasPlaybackReporting())
		{
			return;
		}

		if(navigationViewService.Key is not null)
		{
			return;
		}

		navigationViewService.AddNavigationItem(new Core.Settings.CustomNavigationViewItem
		{
			Key = typeof(PlaybackReportingDashboardViewModel).FullName!,
			Name = "Reporting",
			Glyph = "\uE9F9",
		});
	}

	public void ConfigurePages(IPageRegistration pageRegistration)
	{
		pageRegistration.Configure<PlaybackReportingDashboardViewModel, PlaybackReportingDashboardPage>();
		pageRegistration.Configure<UsersReportViewModel, UsersReportPage>();
		pageRegistration.Configure<PlaybackReportViewModel, PlaybackReportPage>();
		pageRegistration.Configure<BreakdownReportViewModel, BreakdownReportPage>();
		pageRegistration.Configure<UsageReportViewModel, UsageReportPage>();
		pageRegistration.Configure<SessionDurationReportViewModel, SessionDurationReportPage>();
	}

	public void ConfigureServices(IServiceCollection services)
	{
		services.AddTransient<PlaybackReportingDashboardViewModel>();
		services.AddTransient<UsersReportViewModel>();
		services.AddTransient<PlaybackReportViewModel>();
		services.AddTransient<BreakdownReportViewModel>();
		services.AddTransient<UsageReportViewModel>();
		services.AddTransient<SessionDurationReportViewModel>();
	}
}
