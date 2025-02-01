using FluentFin.Core;
using FluentFin.Plugins.Playback_Reporting.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace FluentFin.Plugins.Playback_Reporting.Views;

public sealed partial class PlaybackReportingUsersPage : Page
{
	public PlaybackReportingUsersViewModel ViewModel { get; } = Locator.GetService<PlaybackReportingUsersViewModel>();

	public PlaybackReportingUsersPage()
	{
		InitializeComponent();
	}
}
