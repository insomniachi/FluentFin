using FluentFin.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace FluentFin.Views;

public sealed partial class VideoPlayerPage : Page
{
	public VideoPlayerViewModel ViewModel { get; } = App.GetService<VideoPlayerViewModel>();

    public VideoPlayerPage()
	{
		InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
		ViewModel.ToggleFullScreen = () => MediaPlayerHost.OnPlayerDoubleTapped(MediaPlayerHost, new Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs());
    }
}
