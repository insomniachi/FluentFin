using FluentFin.Core;
using FluentFin.Plugins.Playback_Reporting.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace FluentFin.Plugins.Playback_Reporting.Views;

public sealed partial class SessionDurationReportPage : Page
{
	public SessionDurationReportViewModel ViewModel { get; } = Locator.GetService<SessionDurationReportViewModel>();

	public SessionDurationReportPage()
	{
		InitializeComponent();
	}
}
