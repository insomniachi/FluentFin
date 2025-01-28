using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml.Controls;

namespace FluentFin.Controls;

public partial class MenuFlyoutButton : Button
{
	[GeneratedDependencyProperty]
	public partial IconElement? Icon { get; set; }

	public MenuFlyoutButton()
	{
		DefaultStyleKey = typeof(MenuFlyoutButton);
	}
}
