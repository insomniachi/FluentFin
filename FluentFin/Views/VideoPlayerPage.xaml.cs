using FluentFin.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace FluentFin.Views;

public sealed partial class VideoPlayerPage : Page
{
	public VideoPlayerViewModel ViewModel { get; } = App.GetService<VideoPlayerViewModel>();

	public VideoPlayerPage()
	{
		InitializeComponent();

		Loaded += VideoPlayerPage_Loaded;
	}

	private void VideoPlayerPage_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
	{
		MediaPlayerElement.SetMediaPlayer(ViewModel.MediaPlayer);
	}
}
