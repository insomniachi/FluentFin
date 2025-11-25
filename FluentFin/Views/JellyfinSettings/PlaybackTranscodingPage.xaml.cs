using FluentFin.Core.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace FluentFin.Views.JellyfinSettings;

public sealed partial class PlaybackTranscodingPage : Page
{
	public PlaybackTranscodingViewModel ViewModel { get; } = App.GetService<PlaybackTranscodingViewModel>();

	public PlaybackTranscodingPage()
	{
		InitializeComponent();
	}
}
