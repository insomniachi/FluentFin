using FluentFin.Core.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace FluentFin.Views.JellyfinSettings;

public sealed partial class PlaybackTrickplayPage : Page
{
	public PlaybackTrickplayViewModel ViewModel { get; } = App.GetService<PlaybackTrickplayViewModel>();

	public PlaybackTrickplayPage()
	{
		InitializeComponent();
	}
}
