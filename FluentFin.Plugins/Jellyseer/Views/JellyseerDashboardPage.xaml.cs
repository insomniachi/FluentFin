using FluentFin.Core;
using FluentFin.Plugins.Jellyseer.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace FluentFin.Plugins.Jellyseer.Views;

public sealed partial class JellyseerDashboardPage : Page
{
	public JellyseerDashboardViewModel ViewModel { get; } = Locator.GetService<JellyseerDashboardViewModel>();

	public JellyseerDashboardPage()
	{
		InitializeComponent();
	}
}
