﻿using System.Reactive.Linq;
using FluentFin.Contracts.Services;
using FluentFin.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using ReactiveUI;
using Windows.System;

namespace FluentFin.Views;

public sealed partial class ShellPage : Page
{
	public ShellViewModel ViewModel
	{
		get;
	}

	public ShellPage()
	{
		ViewModel = App.GetService<ShellViewModel>();
		InitializeComponent();

		ViewModel.NavigationService.Frame = NavigationFrame;
		ViewModel.NavigationViewService.Initialize(NavigationViewControl);

		ViewModel.WhenAnyValue(x => x.Selected)
			.WhereNotNull()
			.Where(x => x is NavigationViewItem)
			.ObserveOn(RxApp.MainThreadScheduler)
			.Subscribe(item =>
			{
				NavigationViewControl.SelectedItem = NavigationViewControl.MenuItems.OfType<NavigationViewItem>().FirstOrDefault(x => x.Content == ((NavigationViewItem)item).Content);
			});
	}

	private void OnLoaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
	{
		KeyboardAccelerators.Add(BuildKeyboardAccelerator(VirtualKey.Left, VirtualKeyModifiers.Menu));
		KeyboardAccelerators.Add(BuildKeyboardAccelerator(VirtualKey.GoBack));
	}


	private static KeyboardAccelerator BuildKeyboardAccelerator(VirtualKey key, VirtualKeyModifiers? modifiers = null)
	{
		var keyboardAccelerator = new KeyboardAccelerator() { Key = key };

		if (modifiers.HasValue)
		{
			keyboardAccelerator.Modifiers = modifiers.Value;
		}

		keyboardAccelerator.Invoked += OnKeyboardAcceleratorInvoked;

		return keyboardAccelerator;
	}

	private static void OnKeyboardAcceleratorInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
	{
		var navigationService = App.GetService<INavigationService>();

		var result = navigationService.GoBack();

		args.Handled = result;
	}

	private async void OnContributeTapped(object sender, TappedRoutedEventArgs e)
	{
		await Launcher.LaunchUriAsync(new Uri("https://github.com/insomniachi/FluentFin"));
	}
}
