using FluentFin.Core.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace FluentFin.Views.JellyfinSettings;

public sealed partial class ActivitiesPage : Page
{
	public ActivitiesViewModel ViewModel { get; } = App.GetService<ActivitiesViewModel>();

	public ActivitiesPage()
	{
		InitializeComponent();
	}
}
