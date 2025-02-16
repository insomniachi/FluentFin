using CommunityToolkit.Mvvm.ComponentModel;

using FluentFin.Contracts.Services;
using FluentFin.Core.ViewModels;
using FluentFin.Dialogs.ViewModels;
using FluentFin.Plugins.Playback_Reporting.ViewModels;
using FluentFin.Plugins.Playback_Reporting.Views;
using FluentFin.ViewModels;
using FluentFin.Views;
using FluentFin.Views.JellyfinSettings;
using Microsoft.UI.Xaml.Controls;
using System.ComponentModel;

namespace FluentFin.Services;

public class PageService : IPageService
{
	private readonly Dictionary<string, Type> _pages = new();
	private readonly Dictionary<Type, Type> _viewModels = new();
	private readonly Dictionary<Type, Type> _parents = new();
	private readonly Lock _lock = new();

    public PageService()
	{
		// Setup Section
		Configure<ShellViewModel, ShellPage>();
		Configure<SelectServerViewModel, SelectServerPage>();
		Configure<LoginViewModel, LoginPage>();

		// Main NavView Pages
		Configure<HomeViewModel, HomePage>();
		Configure<LibraryViewModel, LibraryPage>();
		Configure<VideoPlayerViewModel, VideoPlayerPage>();
		Configure<MovieViewModel, MoviePage>();
		Configure<SeriesViewModel, SeriesPage>();
		Configure<SeasonViewModel, SeasonPage>();
		Configure<JellyfinSettingsViewModel, JellyfinSettingsPage>();
		Configure<SettingsViewModel, SettingsPage>();
		Configure<MediaSegmentsEditorViewModel, MediaSegmentsEditorPage>();
		Configure<LibrariesLandingPageViewModel, LibrariesLandingPage>();
		Configure<EpisodeViewModel, EpisodePage>();

		// Dashboard Pages
		Configure<DashboardViewModel, DashboardPage>();
		Configure<GeneralSettingsViewModel, GeneralSettingsPage>();
		Configure<UsersViewModel, UsersPage>();
		Configure<UserEditorViewModel, UserEditorPage>();
		Configure<UserProfileEditorViewModel, UserProfileEditorPage>();
		Configure<UserAccessEditorViewModel, UserAccessEditorPage>();
		Configure<UserParentalControlEditorViewModel, UserParentalControlEditorPage>();
		Configure<UserPasswordEditorViewModel, UserPasswordEditorPage>();
		Configure<LibrariesSettingsViewModel, LibrariesSettingsPage>();
		Configure<LibrariesMetadataViewModel, LibrariesMetadataPage>();
		Configure<LibrariesNfoSettingsViewModel, LibrariesNfoSettingsPage>();
		Configure<LibrariesDisplayViewModel, LibrariesDisplayPage>();
		Configure<PlaybackResumeViewModel, PlaybackResumePage>();
		Configure<PlaybackTrickplayViewModel, PlaybackTrickplayPage>();
		Configure<ActivitiesViewModel, ActivitiesPage>();
		Configure<ScheduledTasksViewModel, ScheduledTasksPage>();

		// Playback Reporting Pages
		Configure<PlaybackReportingDashboardViewModel, PlaybackReportingDashboardPage>();
		Configure<UsersReportViewModel, UsersReportPage>();
		Configure<PlaybackReportViewModel, PlaybackReportPage>();
		Configure<BreakdownReportViewModel, BreakdownReportPage>();
		Configure<UsageReportViewModel, UsageReportPage>();
		Configure<SessionDurationReportViewModel, SessionDurationReportPage>();


		// Setup Parent/Child Relationships
		ConfigureParent<LibraryViewModel, LibrariesLandingPageViewModel>();
    }

    public Type GetPageType(string key)
	{
		Type? pageType;
		lock (_pages)
		{
			if (!_pages.TryGetValue(key, out pageType))
			{
				throw new ArgumentException($"Page not found: {key}. Did you forget to call PageService.Configure?");
			}
		}

		return pageType;
	}

	public Type GetViewModelType(Type type)
	{
        Type? vmType;
        lock (_pages)
        {
            if (!_viewModels.TryGetValue(type, out vmType))
            {
                throw new ArgumentException($"VM not found: {type.FullName}. Did you forget to call PageService.Configure?");
            }
        }

        return vmType;
    }

	public Type? GetParent(Type typeKey)
	{
		if(_parents.TryGetValue(typeKey, out var parent))
		{
			return parent;
		}

		return null;
	}

	private void Configure<VM, V>()
		where VM : ObservableObject
		where V : Page
	{
		lock (_pages)
		{
			var key = typeof(VM).FullName!;
			if (_pages.ContainsKey(key))
			{
				throw new ArgumentException($"The key {key} is already configured in PageService");
			}

			var type = typeof(V);
			if (_pages.ContainsValue(type))
			{
				throw new ArgumentException($"This type is already configured with key {_pages.First(p => p.Value == type).Key}");
			}

			_pages.Add(key, type);
			_viewModels.Add(type, typeof(VM));
		}
	}

    private void ConfigureParent<TChild, TParent>()
    where TChild : INotifyPropertyChanged
    where TParent : INotifyPropertyChanged
    {
        lock (_lock)
        {
			var key = typeof(TChild);
            if(_parents.ContainsKey(key))
            {
                throw new ArgumentException($"The key {key} is already configured in PageService");
            }

			_parents.Add(key, typeof(TParent));
        }
    }
}
