using FluentFin.ViewModels;
using FlyleafLib;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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

		FSC.FullScreenExit += (o, e) =>
		{
			TransportControls.FullWindowSymbol.Symbol = Symbol.FullScreen;
			App.MainWindow.IsShownInSwitchers = true;
			Task.Run(() => { Thread.Sleep(10); Utils.UIInvoke(() => flyleafHost.KFC?.Focus(FocusState.Keyboard)); });
		};

		FSC.FullScreenEnter += (o, e) =>
		{
			TransportControls.FullWindowSymbol.Symbol = Symbol.BackToWindow;
			App.MainWindow.IsShownInSwitchers = false;
			flyleafHost.KFC?.Focus(FocusState.Keyboard);
		};

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
}
