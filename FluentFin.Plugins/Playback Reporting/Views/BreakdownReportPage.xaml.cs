using FluentFin.Core;
using FluentFin.Plugins.Playback_Reporting.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace FluentFin.Plugins.Playback_Reporting.Views;

public sealed partial class BreakdownReportPage : Page
{
	public BreakdownReportViewModel ViewModel { get; } = Locator.GetService<BreakdownReportViewModel>();

	public BreakdownReportPage()
	{
		InitializeComponent();
	}
}
