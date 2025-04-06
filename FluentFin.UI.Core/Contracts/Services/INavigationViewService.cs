using System;
using System.Collections.Generic;
using FluentFin.Core.Services;
using Microsoft.UI.Xaml.Controls;

namespace FluentFin.Contracts.Services;

public interface INavigationViewService : INavigationViewServiceCore
{
	IList<object>? MenuItems
	{
		get;
	}

	object? SettingsItem
	{
		get;
	}

	void Initialize(NavigationView navigationView);

	void UnregisterEvents();

	NavigationViewItem? GetSelectedItem(Type pageType);

	NavigationViewItem? GetSelectedItem();

	void TogglePane();
}
