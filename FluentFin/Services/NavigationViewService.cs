using FluentFin.Contracts.Services;
using FluentFin.Core;
using FluentFin.Core.Contracts.Services;
using FluentFin.Core.Services;
using FluentFin.Core.Settings;
using FluentFin.Core.ViewModels;
using FluentFin.UI.Core;
using FluentFin.UI.Core.Contracts.Services;
using FluentFin.Views;
using Microsoft.UI.Xaml.Controls;

namespace FluentFin.Services;

public class NavigationViewService(INavigationService navigationService,
								   IPageService pageService,
								   ILocalSettingsService localSettingsService,
								   IPluginManager pluginManager) : INavigationViewService
{
	private NavigationView? _navigationView;
	private readonly List<CustomNavigationViewItem> _customNavigationItems = [];

	public IList<object>? MenuItems => _navigationView?.MenuItems;

	public object? SettingsItem => _navigationView?.SettingsItem;

	public object? Key { get; init; }

	public void Initialize(NavigationView navigationView)
	{
		if (_navigationView is not null)
		{
			_navigationView.BackRequested -= OnBackRequested;
			_navigationView.ItemInvoked -= OnItemInvoked;
			_navigationView = null;
		}

		_navigationView = navigationView;
		_navigationView.BackRequested += OnBackRequested;
		_navigationView.ItemInvoked += OnItemInvoked;

		pluginManager.AddNavigationItems(this);

		var customItems = localSettingsService.ReadSetting<List<CustomNavigationViewItem>>($"{Key}_Items", []);
		foreach (var item in customItems)
		{
			AddNavigationItem(item);
		}
	}

	public void SaveCustomViews()
	{
		localSettingsService.SaveSetting($"{Key}_Items", _customNavigationItems.Where(x => x.Persistent).ToList());
	}

	public void RemoveNavigationItem(CustomNavigationViewItem item)
	{
		if (_navigationView is null)
		{
			return;
		}

		if (_navigationView.MenuItems.OfType<NavigationViewItem>().FirstOrDefault(i => i.Tag?.Equals(item) == true) is not { } nav)
		{
			return;
		}

		_navigationView.MenuItems.Remove(nav);
		_customNavigationItems.Remove(item);
		SaveCustomViews();
	}

	public void AddNavigationItem(CustomNavigationViewItem item)
	{
		if (_navigationView is null)
		{
			return;
		}

		var nav = new NavigationViewItem
		{
			Content = item.Name,
			Icon = new FontIcon { Glyph = item.Glyph ?? "\uE700" },
			Tag = item,
		};

		if(item.Commands.Count > 0)
		{
			var flyout = new MenuFlyout();
			foreach (var command in item.Commands)
			{
				flyout.Items.Add(new MenuFlyoutItem
				{
					Text = command.DisplayName,
					Icon = new FontIcon { Glyph = command.Glyph },
					Command = CommandFactory.FindByName(command.Name),
					CommandParameter = item,
				});
			}

			nav.ContextFlyout = flyout;
		}

		NavigationHelper.SetNavigateTo(nav, item.Key);

		_navigationView.MenuItems.Add(nav);
		_customNavigationItems.Add(item);
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
		if (_navigationView == null)
		{
			return null;
		}

		if (pageType == typeof(LibraryPage) && GetSelectedItem() is { } nav && nav.Tag is CustomNavigationViewItem)
		{
			return nav;
		}
		else
		{
			return GetSelectedItem([.. _navigationView.MenuItems, .. _navigationView.FooterMenuItems], pageType);
		}
	}

	public NavigationViewItem? GetSelectedItem() => (NavigationViewItem?)_navigationView?.SelectedItem;

	private void OnBackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args) => navigationService.GoBack();

	private void OnItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
	{
		if (args.IsSettingsInvoked)
		{
			navigationService.NavigateTo<SettingsViewModel>();
		}
		else
		{
			if (args.InvokedItemContainer is not NavigationViewItem selectedItem)
			{
				return;
			}

			if (selectedItem.GetValue(NavigationHelper.NavigateToProperty) is string pageKey)
			{
				if(selectedItem.Tag is CustomNavigationViewItem { Parameter: not null } item)
				{
					navigationService.NavigateTo(pageKey, item.Parameter);
				}
				else
				{
					navigationService.NavigateTo(pageKey);
				}
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
