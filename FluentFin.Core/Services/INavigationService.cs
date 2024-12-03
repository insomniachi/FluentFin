namespace FluentFin.Contracts.Services;

public interface INavigationServiceCore
{
    bool CanGoBack { get; }

    bool NavigateTo(string pageKey, object? parameter = null, bool clearNavigation = false);

    bool GoBack();
}
