namespace FluentFin.Core.Services;

public interface INavigationServiceCore
{
	bool CanGoBack { get; }

	bool NavigateTo(string pageKey, object? parameter = null, bool clearNavigation = false);

	bool GoBack();
}

public static class NavigationServiceExtensions
{
	public static bool NavigateTo<T>(this INavigationServiceCore navigationService, object? parameter = null, bool clearNavigation = false)
	{
		return navigationService.NavigateTo(typeof(T).FullName!, parameter, clearNavigation);
	}
}
