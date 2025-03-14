using FluentFin.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace FluentFin.Views;

public sealed partial class VideoPlayerPage : Page
{
	public VideoPlayerViewModel ViewModel { get; } = App.GetService<VideoPlayerViewModel>();

	public VideoPlayerPage()
	{
		InitializeComponent();
	}
}
