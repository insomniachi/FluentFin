using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using ReactiveUI;
using System.Reactive.Linq;

namespace FluentFin.Services;

public interface IContentDialogService
{
	Task<ContentDialogResult> ShowDialog<TViewModel>(Action<ContentDialog> configure) where TViewModel : class;
	Task<ContentDialogResult> ShowDialog<TViewModel>(TViewModel viewModel, Action<ContentDialog> configure) where TViewModel : class;
	Task<ContentDialogResult> ShowDialog<TViewModel>(Action<ContentDialog> configure, Action<TViewModel> configureVm) where TViewModel : class;
	Task<ContentDialogResult> ShowDialog<TView, TViewModel>(TViewModel viewModel, Action<ContentDialog> configure) where TView : ContentDialog, IViewFor, new();
	Task ShowMessage(string title, string message, TimeSpan? timeout = null);
	Task<bool> QuestionYesNo(string title, string message);
}

public class ContentDialogService : IContentDialogService
{
	public async Task<ContentDialogResult> ShowDialog<TViewModel>(Action<ContentDialog> configure)
		where TViewModel : class
	{
		var vm = App.GetService<TViewModel>();
		return await ShowDialog(vm, configure);
	}

	public async Task<bool> QuestionYesNo(string title, string message)
	{
		var dialog = new ContentDialog
		{
			XamlRoot = App.MainWindow.Content.XamlRoot,
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
			XamlRoot = App.MainWindow.Content.XamlRoot,
			Title = title,
			Content = message,
			PrimaryButtonText = "OK",
			DefaultButton = ContentDialogButton.Primary
		};

		if (timeout is not null)
		{
			Observable.Timer(timeout.Value).ObserveOn(RxApp.MainThreadScheduler).Subscribe(_ => dialog.Hide());
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
		dialog.XamlRoot = App.MainWindow.Content.XamlRoot;

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
		dialog.XamlRoot = App.MainWindow.Content.XamlRoot;

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
