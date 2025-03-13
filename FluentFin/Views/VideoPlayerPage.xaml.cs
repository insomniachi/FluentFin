using CommunityToolkit.WinUI;
using FluentFin.Core.ViewModels;
using FluentFin.ViewModels;
using LibVLCSharp.Shared;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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

		_pointerMoved
			.Throttle(TimeSpan.FromSeconds(3))
			.Subscribe(_ =>
			{
				TransportControls.Bar.DispatcherQueue.TryEnqueue(() =>
				{
					TransportControls.Bar.Visibility = Visibility.Collapsed;
					TransportControls.TitleSection.Visibility = Visibility.Collapsed;
					ProtectedCursor.Dispose();
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
			TransportControls.TitleSection.Visibility = Visibility.Visible;
			TransportControls.TxtTitleTime.Text = DateTime.Now.ToString("hh:mm tt");
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

		if (this.FindAscendant<NavigationView>() is { } navView)
		{
			navView.IsPaneVisible ^= true;
		}

		App.MainWindow.AppWindow.SetPresenter(presenterKind);
	}

    private void VideoView_Initialized(object sender, LibVLCSharp.Platforms.Windows.InitializedEventArgs e)
    {
		ViewModel.SetMediaPlayer(e.SwapChainOptions);
		VideoView.MediaPlayer = ViewModel.MediaPlayer;
    }
}
