using FluentFin.Core;
using FluentFin.Plugins.Playback_Reporting.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace FluentFin.Plugins.Playback_Reporting.Views;

public sealed partial class UsersReportPage : Page
{
	public UsersReportViewModel ViewModel { get; } = Locator.GetService<UsersReportViewModel>();

	public UsersReportPage()
	{
		InitializeComponent();
	}
}
