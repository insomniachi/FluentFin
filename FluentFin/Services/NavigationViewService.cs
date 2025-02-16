using FluentFin.Contracts.Services;
using FluentFin.Core.Services;
using FluentFin.Core.ViewModels;
using FluentFin.UI.Core;
using Microsoft.UI.Xaml.Controls;
using System.Reflection;

namespace FluentFin.Services;

public class NavigationViewService(INavigationService navigationService,
								   IPageService pageService) : INavigationViewService
{
	private NavigationView? _navigationView;

	public IList<object>? MenuItems => _navigationView?.MenuItems;

	public object? SettingsItem => _navigationView?.SettingsItem;

	public void Initialize(NavigationView navigationView)
	{
		if(_navigationView is not null)
		{
			_navigationView.BackRequested -= OnBackRequested;
			_navigationView.ItemInvoked -= OnItemInvoked;
			_navigationView = null;
		}

		_navigationView = navigationView;
		_navigationView.BackRequested += OnBackRequested;
		_navigationView.ItemInvoked += OnItemInvoked;
	}

	public void TogglePane()
	{
		if (_navigationView is null)
		{
			return;
		}

		_navigationView.IsPaneOpen ^= true;
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
			return GetSelectedItem([.. _navigationView.MenuItems, .. _navigationView.FooterMenuItems], pageType);
		}

		return null;
	}

	private void OnBackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args) => navigationService.GoBack();

	private void OnItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
	{
		if (args.IsSettingsInvoked)
		{
			navigationService.NavigateTo<SettingsViewModel>();
		}
		else
		{
			var selectedItem = args.InvokedItemContainer as NavigationViewItem;

			if (selectedItem?.GetValue(NavigationHelper.NavigateToProperty) is string pageKey)
			{
				navigationService.NavigateTo(pageKey);
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
			var pageType = pageService.GetPageType(pageKey);
			var vmTYpe = pageService.GetViewModelType(pageType);
            return pageType == sourcePageType || IsParent(vmTYpe, sourcePageType);
		}

		return false;
	}

	private bool IsParent(Type parentVmType, Type sourcePageType)
	{
		var vmType = pageService.GetViewModelType(sourcePageType);
		var parentType = pageService.GetParent(vmType);

		return parentType == parentVmType;
    }
}
