using CommunityToolkit.Mvvm.ComponentModel;

using FluentFin.Contracts.Services;
using FluentFin.Core.ViewModels;
using FluentFin.Dialogs.ViewModels;
using FluentFin.Dialogs.Views;
using FluentFin.ViewModels;
using FluentFin.Views;
using FluentFin.Views.JellyfinSettings;
using Microsoft.UI.Xaml.Controls;

namespace FluentFin.Services;

public class PageService : IPageService
{
    private readonly Dictionary<string, Type> _pages = new();

    public PageService()
    {
		Configure<HomeViewModel, HomePage>();
		Configure<LibraryViewModel, LibraryPage>();
		Configure<VideoPlayerViewModel, VideoPlayerPage>();
		Configure<MovieViewModel, MoviePage>();
		Configure<SeriesViewModel, SeriesPage>();
		Configure<SeasonViewModel, SeasonPage>();
        Configure<JellyfinSettingsViewModel, JellyfinSettingsPage>();

        // Settings Pages
        Configure<DashboardViewModel, DashboardPage>();
        Configure<UsersViewModel, UsersPage>();
		Configure<UserEditorViewModel, UserEditorPage>();
        Configure<UserProfileEditorViewModel, UserProfileEditorPage>();
        Configure<UserAccessEditorViewModel, UserAccessEditorPage>();
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
        }
    }
}
