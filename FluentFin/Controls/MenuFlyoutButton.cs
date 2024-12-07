using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace FluentFin.Controls;

public partial class MenuFlyoutButton : Button
{
	public IconElement Icon
	{
		get { return (IconElement)GetValue(IconProperty); }
		set { SetValue(IconProperty, value); }
	}

	public static readonly DependencyProperty IconProperty =
		DependencyProperty.Register("Icon", typeof(IconElement), typeof(MenuFlyoutButton), new PropertyMetadata(null));

	public MenuFlyoutButton()
	{
		DefaultStyleKey = typeof(MenuFlyoutButton);
	}
}
