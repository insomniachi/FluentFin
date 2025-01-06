using CommunityToolkit.WinUI;
using FluentFin.Core.ViewModels;
using FluentFin.ViewModels;
using FlyleafLib;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using ReactiveUI;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace FluentFin.Views;

public sealed partial class VideoPlayerPage : Page
{
	public VideoPlayerViewModel ViewModel { get; } = App.GetService<VideoPlayerViewModel>();
	private readonly Subject<Unit> _pointerMoved = new();

	public VideoPlayerPage()
	{
		InitializeComponent();

		//FSC.FullScreenExit += (o, e) =>
		//{
		//	TransportControls.FullWindowSymbol.Symbol = Symbol.FullScreen;
		//	App.MainWindow.IsShownInSwitchers = true;
		//	Task.Run(() => { Thread.Sleep(10); Utils.UIInvoke(() => flyleafHost.KFC?.Focus(FocusState.Keyboard)); });
		//};

		//FSC.FullScreenEnter += (o, e) =>
		//{
		//	TransportControls.FullWindowSymbol.Symbol = Symbol.BackToWindow;
		//	App.MainWindow.IsShownInSwitchers = false;
		//	flyleafHost.KFC?.Focus(FocusState.Keyboard);
		//};

		_pointerMoved
			.Throttle(TimeSpan.FromSeconds(3))
			.Subscribe(_ =>
			{
				TransportControls.Bar.DispatcherQueue.TryEnqueue(() =>
				{
					TransportControls.Bar.Visibility = Visibility.Collapsed;
					ProtectedCursor.Dispose();
				});
			});

		ViewModel.WhenAnyValue(x => x.IsSkipButtonVisible)
			.Subscribe(value =>
			{
				TransportControls?.DispatcherQueue.TryEnqueue(() =>
				{
					TransportControls.IsSkipButtonVisible = value;
				});
			});

		TransportControls?.DispatcherQueue.TryEnqueue(() =>
		{
			TransportControls.Trickplay = ViewModel.TrickplayViewModel;
		});

		ViewModel.TrickplayViewModel.WhenAnyValue(x => x.TileImage)
			.WhereNotNull()
			.Subscribe(url =>
			{
				TransportControls?.TrickplayImage.DispatcherQueue.TryEnqueue(() =>
				{
					TransportControls.TrickplayImage.Source = new BitmapImage(url);
				});
			});

		ViewModel.TrickplayViewModel.WhenAnyValue(x => x.Translate)
			.WhereNotNull()
			.Subscribe(translate =>
			{
				TransportControls?.TranslateTransform.DispatcherQueue.TryEnqueue(() =>
				{
					TransportControls.TranslateTransform.X = translate.X;
					TransportControls.TranslateTransform.Y = translate.Y;
				});
			});

		ViewModel.TrickplayViewModel.WhenAnyValue(x => x.Clip)
			.WhereNotNull()
			.Subscribe(clip =>
			{
				TransportControls?.ClipGeometry.DispatcherQueue.TryEnqueue(() =>
				{
					TransportControls.ClipGeometry.Rect = new Windows.Foundation.Rect(clip.X, clip.Y, clip.Width, clip.Height);
				});
			});

		TransportControls!.FullWindowButton.Click += (sender, e) => OnPlayerDoubleTapped(sender, null!);
	}

	private void FSC_PointerMoved(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
	{
		ShowTransportControls();
	}

	private void ShowTransportControls()
	{
		TransportControls.Bar.DispatcherQueue.TryEnqueue(() =>
		{
			TransportControls.Bar.Visibility = Visibility.Visible;
			ProtectedCursor = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
		});

		_pointerMoved.OnNext(Unit.Default);
	}

	private void OnPlayerDoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
	{
		var current = App.MainWindow.AppWindow.Presenter.Kind;
		var presenterKind = current == AppWindowPresenterKind.Overlapped ? AppWindowPresenterKind.FullScreen : AppWindowPresenterKind.Overlapped;

		TransportControls.FullWindowSymbol.Symbol = presenterKind == AppWindowPresenterKind.FullScreen ? Symbol.BackToWindow : Symbol.FullScreen;

		if (App.GetService<ITitleBarViewModel>() is { } vm)
		{
			vm.IsVisible ^= true;
		}

		if(this.FindAscendant<NavigationView>() is { } navView)
		{
			navView.IsPaneVisible ^= true;
		}

		App.MainWindow.AppWindow.SetPresenter(presenterKind);

		flyleafHost.KFC?.Focus(FocusState.Keyboard);
	}
}
