using FluentFin.Core.Settings;

namespace FluentFin.Core.Services;

public interface INavigationServiceCore
{
	bool CanGoBack { get; }
	bool NavigateTo(string pageKey, object? parameter = null, bool clearNavigation = false);
	bool GoBack();
}

public interface INavigationViewServiceCore
{
	object? Key { get; }
	void AddNavigationItem(CustomNavigationViewItem item);
	void RemoveNavigationItem(CustomNavigationViewItem item);
	void SaveCustomViews();
}

public static class NavigationServiceExtensions
{
	public static bool NavigateTo<T>(this INavigationServiceCore navigationService, object? parameter = null, bool clearNavigation = false)
	{
		return navigationService.NavigateTo(typeof(T).FullName!, parameter, clearNavigation);
	}
}
