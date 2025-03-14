using CommunityToolkit.WinUI;
using FluentFin.Core.ViewModels;
using FluentFin.MediaPlayers;
using FluentFin.MediaPlayers.Flyleaf;
using FluentFin.MediaPlayers.Vlc;
using FluentFin.ViewModels;
using FlyleafLib.Controls.WinUI;
using LibVLCSharp.Platforms.Windows;
using Microsoft.UI.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ReactiveUI;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows.Input;


namespace FluentFin.Controls;

public sealed partial class MediaPlayerHost : UserControl
{
    private readonly Subject<Unit> _pointerMoved = new();

    [GeneratedDependencyProperty]
    public partial bool IsSkipButtonVisible { get; set; }

    [GeneratedDependencyProperty]
    public partial PlaylistViewModel? Playlist { get; set; }

    [GeneratedDependencyProperty]
    public partial ICommand? SkipCommand { get; set; }

    [GeneratedDependencyProperty]
    public partial TrickplayViewModel? Trickplay { get; set; }

    [GeneratedDependencyProperty]
    public partial MediaPlayerType? MediaPlayerType { get; set; }

    public IMediaPlayerController? Player
    {
        get
        {
            try
            {
                return (IMediaPlayerController)GetValue(PlayerProperty);
            }
            catch
            {
                return null;
            }
        }
        set { SetValue(PlayerProperty, value); }
    }

    public static readonly DependencyProperty PlayerProperty =
        DependencyProperty.Register("Player", typeof(IMediaPlayerController), typeof(MediaPlayerHost), new PropertyMetadata(null));

    public MediaPlayerHost()
    {
        InitializeComponent();

        this.WhenAnyValue(x => x.MediaPlayerType)
            .WhereNotNull()
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(type =>
            {
                if(RootGrid.Children.Count > 1)
                {
                    RootGrid.Children.RemoveAt(0);
                }

                UIElement host = type switch
                {
                    Controls.MediaPlayerType.Vlc => CreateVLC(),
                    Controls.MediaPlayerType.Flyleaf => CreateFlyleaf(),
                    Controls.MediaPlayerType.WindowsMediaPlayer => CreateWindowsMediaPlayer(),
                    _ => throw new NotImplementedException()
                };

                RootGrid.Children.Insert(0, host);
            });

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

    private MediaPlayerElement CreateWindowsMediaPlayer()
    {
        var element = new MediaPlayerElement();
        element.Loaded += Element_Loaded;
        return element;
    }

    private FlyleafHost CreateFlyleaf()
    {
        var host = new FlyleafHost();
        host.Loaded += Host_Loaded;
        Player = new FlyleafMediaPlayerController(host);
        return host;
    }

    private VideoView CreateVLC()
    {
        var view = new VideoView();
        view.Initialized += VLCInitialized;
        return view;
    }

    private void VLCInitialized(object? sender, InitializedEventArgs e)
    {
        if(sender is not VideoView view)
        {
            return;
        }

        DispatcherQueue.TryEnqueue(() =>
        {
            Player = new VlcMediaPlayerController(view, e.SwapChainOptions, TransportControls.AudioSelectionButton);
        });
    }

    private void Host_Loaded(object sender, RoutedEventArgs e)
    {
        if(sender is not FlyleafHost host)
        {
            return;
        }

        DispatcherQueue.TryEnqueue(() =>
        {
            Player = new FlyleafMediaPlayerController(host);
        });
    }

    private void Element_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is not MediaPlayerElement element)
        {
            return;
        }

        DispatcherQueue.TryEnqueue(() =>
        {
            Player = new WindowsMediaPlayerController(element);
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
}

public enum MediaPlayerType
{
    Vlc,
    Flyleaf,
    WindowsMediaPlayer
}
