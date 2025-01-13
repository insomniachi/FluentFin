using CommunityToolkit.WinUI;
using FluentFin.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;


namespace FluentFin.Views;

public sealed partial class MediaSegmentsEditorPage : Page
{
	public MediaSegmentsEditorViewModel ViewModel { get; } = App.GetService<MediaSegmentsEditorViewModel>();

	public MediaSegmentsEditorPage()
	{
		InitializeComponent();
	}

	private void UpdateTime(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
	{
		if(sender is not RepeatButton b)
		{
			return;
		}

		if(b.FindAscendant<Grid>()?.FindDescendant<NumberBox>() is { } numberBox)
		{
			numberBox.Value = ViewModel.CurrentTimeTicks;
		}
	}
}
