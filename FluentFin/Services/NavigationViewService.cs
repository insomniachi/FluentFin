using FluentFin.Contracts.Services;
using FluentFin.Core.Contracts.Services;
using FluentFin.Core.ViewModels;
using FluentFin.Helpers;
using Jellyfin.Sdk.Generated.Models;
using Microsoft.UI.Xaml.Controls;
using System.Diagnostics.CodeAnalysis;

namespace FluentFin.Services;

public class NavigationViewService : INavigationViewService
{
    private readonly INavigationService _navigationService;
    private readonly IPageService _pageService;
	private readonly IJellyfinClient _jellyfinClient;
    private NavigationView? _navigationView;

    public IList<object>? MenuItems => _navigationView?.MenuItems;

    public object? SettingsItem => _navigationView?.SettingsItem;

    public NavigationViewService(INavigationService navigationService,
								 IPageService pageService,
								 IJellyfinClient jellyfinClient)
    {
        _navigationService = navigationService;
        _pageService = pageService;
		_jellyfinClient = jellyfinClient;
    }

    [MemberNotNull(nameof(_navigationView))]
    public void Initialize(NavigationView navigationView)
    {
        _navigationView = navigationView;
        _navigationView.BackRequested += OnBackRequested;
        _navigationView.ItemInvoked += OnItemInvoked;
    }

	public void TogglePane()
	{
		if(_navigationView is null)
		{
			return;
		}

		_navigationView.IsPaneOpen ^= true;
	}

	public async Task InitializeLibraries()
	{
		var libarariesItem = new NavigationViewItem 
		{
			Content = "Libraries",
			Icon = new SymbolIcon
			{
				Symbol = Symbol.Library
			}, 
			SelectsOnInvoked = false
		};
		await foreach (var item in _jellyfinClient.GetUserLibraries())
		{
			var libraryItem = new NavigationViewItem
			{
				Content = item.Name,
				Icon = GetIcon(item.CollectionType),
				Tag = item
			};
			libraryItem.Tapped += LibarariesItem_Tapped;

			libarariesItem.MenuItems.Add(libraryItem);
		}

		MenuItems?.Add(libarariesItem);
	}

	private void LibarariesItem_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
	{
		var dto = (BaseItemDto)((NavigationViewItem)sender).Tag;
		_navigationService.NavigateTo(typeof(LibraryViewModel).FullName!, dto);
	}

	public void UnregisterEvents()
    {
        if (_navigationView != null)
        {
            _navigationView.BackRequested -= OnBackRequested;
            _navigationView.ItemInvoked -= OnItemInvoked;
        }
    }

    public NavigationViewItem? GetSelectedItem(Type pageType)
    {
        if (_navigationView != null)
        {
            return GetSelectedItem(_navigationView.MenuItems, pageType) ?? GetSelectedItem(_navigationView.FooterMenuItems, pageType);
        }

        return null;
    }

    private void OnBackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args) => _navigationService.GoBack();

    private void OnItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.IsSettingsInvoked)
        {
            // Navigate to the settings page.
        }
        else
        {
            var selectedItem = args.InvokedItemContainer as NavigationViewItem;

            if (selectedItem?.GetValue(NavigationHelper.NavigateToProperty) is string pageKey)
            {
                _navigationService.NavigateTo(pageKey);
            }
        }
    }

    private NavigationViewItem? GetSelectedItem(IEnumerable<object> menuItems, Type pageType)
    {
        foreach (var item in menuItems.OfType<NavigationViewItem>())
        {
            if (IsMenuItemForPageType(item, pageType))
            {
                return item;
            }

            var selectedChild = GetSelectedItem(item.MenuItems, pageType);
            if (selectedChild != null)
            {
                return selectedChild;
            }
        }

        return null;
    }

    private bool IsMenuItemForPageType(NavigationViewItem menuItem, Type sourcePageType)
    {
        if (menuItem.GetValue(NavigationHelper.NavigateToProperty) is string pageKey)
        {
            return _pageService.GetPageType(pageKey) == sourcePageType;
        }

        return false;
    }

	private static FontIcon? GetIcon(BaseItemDto_CollectionType? collectionType)
	{
		return collectionType switch
		{
			BaseItemDto_CollectionType.Movies => new FontIcon { Glyph = "\uE8B2" },
			BaseItemDto_CollectionType.Tvshows => new FontIcon { Glyph = "\uE7F4" },
			BaseItemDto_CollectionType.Boxsets => new FontIcon { Glyph = "\uF133" },
			_ => null
		};
	}
}
