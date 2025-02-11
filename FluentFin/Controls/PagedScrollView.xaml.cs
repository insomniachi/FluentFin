using System.Collections.ObjectModel;
using CommunityToolkit.WinUI;
using FluentFin.Core.Contracts.Services;
using FluentFin.Core.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;


namespace FluentFin.Controls;

public sealed partial class PagedScrollView : UserControl
{

	[GeneratedDependencyProperty(DefaultValue = "")]
	public partial string Header { get; set; }

	[GeneratedDependencyProperty(DefaultValueCallback = nameof(Empty))]
	public partial ObservableCollection<BaseItemViewModel> Items { get; set; }

	[GeneratedDependencyProperty]
	public partial IJellyfinClient? JellyfinClient { get; set; }


	public PagedScrollView()
	{
		InitializeComponent();
	}

	private void Scroller_SizeChanged(object sender, SizeChangedEventArgs e)
	{
		UpdateScrollButtonsVisibility();
	}

	private void Scroller_ViewChanging(object sender, ScrollViewerViewChangingEventArgs e)
	{
		if (e.FinalView.HorizontalOffset < 1)
		{
			ScrollLeftBtn.Visibility = Visibility.Collapsed;
		}
		else if (e.FinalView.HorizontalOffset > 1)
		{
			ScrollLeftBtn.Visibility = Visibility.Visible;
		}

		if (e.FinalView.HorizontalOffset > Scroller.ScrollableWidth - 1)
		{
			ScrollRightBtn.Visibility = Visibility.Collapsed;
		}
		else if (e.FinalView.HorizontalOffset < Scroller.ScrollableWidth - 1)
		{
			ScrollRightBtn.Visibility = Visibility.Visible;
		}
	}

	private void ScrollLeftBtn_Click(object sender, RoutedEventArgs e)
	{
		Scroller.ChangeView(Scroller.HorizontalOffset - Scroller.ViewportWidth, null, null);
		ScrollRightBtn.Focus(FocusState.Programmatic);
	}

	private void ScrollRightBtn_Click(object sender, RoutedEventArgs e)
	{
		Scroller.ChangeView(Scroller.HorizontalOffset + Scroller.ViewportWidth, null, null);
		ScrollLeftBtn.Focus(FocusState.Programmatic);
	}

	private void UpdateScrollButtonsVisibility()
	{
		if (Scroller.ScrollableWidth > 0)
		{
			if (Scroller.HorizontalOffset > 1)
			{
				ScrollLeftBtn.Visibility = Visibility.Visible; 
			}
		}
		else
		{
			ScrollRightBtn.Visibility = Visibility.Collapsed;
		}
	}

	private static ObservableCollection<BaseItemViewModel> Empty() => [];
}
