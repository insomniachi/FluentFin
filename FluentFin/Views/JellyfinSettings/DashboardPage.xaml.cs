using FluentFin.Core.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace FluentFin.Views.JellyfinSettings;

public sealed partial class DashboardPage : Page
{
	public DashboardViewModel ViewModel { get; } = App.GetService<DashboardViewModel>();

	public DashboardPage()
	{
		InitializeComponent();
	}
}
