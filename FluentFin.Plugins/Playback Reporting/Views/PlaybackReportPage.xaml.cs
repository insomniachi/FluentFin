using FluentFin.Core;
using FluentFin.Plugins.Playback_Reporting.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace FluentFin.Plugins.Playback_Reporting.Views;

public sealed partial class PlaybackReportPage : Page
{
	public PlaybackReportViewModel ViewModel { get; } = Locator.GetService<PlaybackReportViewModel>();

	public PlaybackReportPage()
	{
		InitializeComponent();
		ViewModel.WinUIPlot = Plot;
	}
}
