using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace FluentFin.Views
{
	/// <summary>
	/// An empty page that can be used on its own or navigated to within a Frame.
	/// </summary>
	public sealed partial class TrickplayTestPage : Page
	{
		public TrickplayTestPage()
		{
			this.InitializeComponent();
		}

		private void Slider_PointerMoved(object sender, PointerRoutedEventArgs e)
		{
			TeachingTip.IsOpen = true;
			var point = e.GetCurrentPoint(TimeSlider);
			var globalPoint = e.GetCurrentPoint(this);
			TeachingTipThumb.Margin = new Thickness(point.Position.X, 0, 0, 0);
			var margin = Math.Min(point.Position.X, TimeSlider.ActualWidth - TeachingTip.Width);
			TeachingTip.PlacementMargin = new Thickness(margin, 0, 0, globalPoint.Position.Y);
		}
	}
}

