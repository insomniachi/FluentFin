using System.Collections.ObjectModel;
using Jellyfin.Sdk.Generated.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;


namespace FluentFin.Controls;

public sealed partial class PagedScrollView : UserControl
{
	public ObservableCollection<BaseItemDto> Items
	{
		get { return (ObservableCollection<BaseItemDto>)GetValue(ItemsProperty); }
		set { SetValue(ItemsProperty, value); }
	}
	
	public string Header
	{
		get { return (string)GetValue(HeaderProperty); }
		set { SetValue(HeaderProperty, value); }
	}

	public static readonly DependencyProperty HeaderProperty =
		DependencyProperty.Register("Header", typeof(string), typeof(PagedScrollView), new PropertyMetadata(""));

	public static readonly DependencyProperty ItemsProperty =
		DependencyProperty.Register("Items", typeof(ObservableCollection<BaseItemDto>), typeof(PagedScrollView), new PropertyMetadata(new()));


	public PagedScrollView()
	{
		this.InitializeComponent();
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

		// Manually focus to ScrollForwardBtn since this button disappears after scrolling to the end.
		ScrollRightBtn.Focus(FocusState.Programmatic);
	}

	private void ScrollRightBtn_Click(object sender, RoutedEventArgs e)
	{
		Scroller.ChangeView(Scroller.HorizontalOffset + Scroller.ViewportWidth, null, null);

		// Manually focus to ScrollBackBtn since this button disappears after scrolling to the end.
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
}
