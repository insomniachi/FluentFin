using FluentFin.Core.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace FluentFin.Views.JellyfinSettings;

public sealed partial class ScheduledTasksPage : Page
{
	public ScheduledTasksViewModel ViewModel { get; } = App.GetService<ScheduledTasksViewModel>();

	public ScheduledTasksPage()
	{
		InitializeComponent();
	}
}
