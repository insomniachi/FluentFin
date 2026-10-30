using System.Reactive.Linq;
using DevWinUI;
using FluentFin.Core.Contracts.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using ReactiveUI;

namespace FluentFin.Services;

public interface IContentDialogService : IContentDialogServiceCore
{
	Task<ContentDialogResult> ShowDialog<TViewModel>(Action<ContentDialog> configure) where TViewModel : class;
	Task<ContentDialogResult> ShowDialog<TViewModel>(TViewModel viewModel, Action<ContentDialog> configure) where TViewModel : class;
	Task<ContentDialogResult> ShowDialog<TViewModel>(Action<ContentDialog> configure, Action<TViewModel> configureVm) where TViewModel : class;
	Task<ContentDialogResult> ShowDialog<TView, TViewModel>(TViewModel viewModel, Action<ContentDialog> configure) where TView : ContentDialog, IViewFor, new();
}

public class ContentDialogService : IContentDialogService
{
	// The main window content has no XamlRoot until it is loaded into the visual tree (e.g. an update prompt shown during startup).
	private static async Task<XamlRoot> GetXamlRoot()
	{
		if (App.MainWindow.Content is not FrameworkElement content)
		{
			return null;
		}

		if (content.XamlRoot is not null)
		{
			return content.XamlRoot;
		}

		var loaded = new TaskCompletionSource();
		content.Loaded += OnLoaded;
		void OnLoaded(object sender, RoutedEventArgs e)
		{
			content.Loaded -= OnLoaded;
			loaded.TrySetResult();
		}

		await loaded.Task;
		return content.XamlRoot;
	}

	public async Task<ContentDialogResult> ShowDialog<TViewModel>(Action<ContentDialog> configure)
		where TViewModel : class
	{
		var vm = App.GetService<TViewModel>();
		return await ShowDialog(vm, configure);
	}

	public void Growl(string title, string message, TimeSpan waitTime)
	{
		DevWinUI.Growl.InfoGlobal(new GrowlInfo
		{
			Message = message,
			WaitTime = waitTime,
			Title = title
		});
	}

	public async Task<bool> QuestionYesNo(string title, string message)
	{
		var dialog = new ContentDialog
		{
			XamlRoot = await GetXamlRoot(),
			Title = title,
			Content = message,
			PrimaryButtonText = "Yes",
			SecondaryButtonText = "Cancel",
			DefaultButton = ContentDialogButton.Primary
		};

		var result = await dialog.ShowAsync();
		return result == ContentDialogResult.Primary;
	}

	public async Task ShowMessage(string title, string message, TimeSpan? timeout = null)
	{
		var dialog = new ContentDialog
		{
			XamlRoot = await GetXamlRoot(),
			Title = title,
			Content = message,
			PrimaryButtonText = "OK",
			DefaultButton = ContentDialogButton.Primary
		};

		if (timeout is not null)
		{
			Observable.Timer(timeout.Value).ObserveOn(RxSchedulers.MainThreadScheduler).Subscribe(_ => dialog.Hide());
		}

		await dialog.ShowAsync();
	}

	public async Task<ContentDialogResult> ShowDialog<TView, TViewModel>(TViewModel viewModel, Action<ContentDialog> configure)
		where TView : ContentDialog, IViewFor, new()
	{
		var view = new TView
		{
			ViewModel = viewModel
		};

		var dialog = (ContentDialog)view;
		dialog.XamlRoot = await GetXamlRoot();

		dialog.ManipulationDelta += delegate (object sender, ManipulationDeltaRoutedEventArgs e)
		{
			if (!e.IsInertial)
			{
				dialog.Margin = new Thickness(dialog.Margin.Left + e.Delta.Translation.X,
											  dialog.Margin.Top + e.Delta.Translation.Y,
											  dialog.Margin.Left - e.Delta.Translation.X,
											  dialog.Margin.Top - e.Delta.Translation.Y);
			}
		};


		configure?.Invoke(dialog);
		var result = await dialog.ShowAsync();
		if (viewModel is IDisposable d)
		{
			d.Dispose();
		}

		return result;
	}

	public async Task<ContentDialogResult> ShowDialog<TViewModel>(TViewModel viewModel, Action<ContentDialog> configure)
		where TViewModel : class
	{

		var view = App.GetService<IViewFor<TViewModel>>();
		view.ViewModel = viewModel;

		var dialog = (ContentDialog)view;
		dialog.XamlRoot = await GetXamlRoot();

		dialog.ManipulationDelta += delegate (object sender, ManipulationDeltaRoutedEventArgs e)
		{
			if (!e.IsInertial)
			{
				dialog.Margin = new Thickness(dialog.Margin.Left + e.Delta.Translation.X,
												dialog.Margin.Top + e.Delta.Translation.Y,
												dialog.Margin.Left - e.Delta.Translation.X,
												dialog.Margin.Top - e.Delta.Translation.Y);
			}
		};

		configure?.Invoke(dialog);
		var result = await dialog.ShowAsync();
		if (viewModel is IDisposable d)
		{
			d.Dispose();
		}

		return result;
	}

	public Task<ContentDialogResult> ShowDialog<TViewModel>(Action<ContentDialog> configure, Action<TViewModel> configureVm) where TViewModel : class
	{
		var vm = App.GetService<TViewModel>();
		configureVm?.Invoke(vm);
		return ShowDialog(vm, configure);
	}
}
