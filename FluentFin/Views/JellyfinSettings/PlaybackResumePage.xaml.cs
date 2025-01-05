using FluentFin.Core.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace FluentFin.Views.JellyfinSettings;

public sealed partial class PlaybackResumePage : Page
{
	public PlaybackResumeViewModel ViewModel { get; } = App.GetService<PlaybackResumeViewModel>();

	public PlaybackResumePage()
	{
		InitializeComponent();
	}
}
