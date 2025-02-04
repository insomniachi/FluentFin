using FluentFin.Core;
using FluentFin.Plugins.Playback_Reporting.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace FluentFin.Plugins.Playback_Reporting.Views;

public sealed partial class UsageReportPage : Page
{
	public UsageReportViewModel ViewModel { get; } = Locator.GetService<UsageReportViewModel>();

	public UsageReportPage()
	{
		InitializeComponent();
	}
}
