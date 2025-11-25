using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;

namespace FluentFin.UI.Core.Contracts.Services;

public interface IPageService
{
	Type GetPageType(string key);
	Type GetViewModelType(Type type);
	Type? GetParent(Type type);
}

public interface IPageRegistration
{
	public void Configure<VM, V>() where VM : ObservableObject where V : Page;
}
