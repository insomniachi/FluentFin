using DevWinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace FluentFin.Controls;

public partial class LoadingImage : ImageEx
{
	public LoadingImage()
	{
		DefaultStyleKey = typeof(LoadingImage);
	}


	public Symbol LoadingSymbol
	{
		get { return (Symbol)GetValue(LoadingSymbolProperty); }
		set { SetValue(LoadingSymbolProperty, value); }
	}

	public static readonly DependencyProperty LoadingSymbolProperty =
		DependencyProperty.Register("LoadingSymbol", typeof(Symbol), typeof(LoadingImage), new PropertyMetadata(Symbol.Pictures));


	public IconElement LoadingIcon
	{
		get { return (IconElement)GetValue(LoadingIconProperty); }
		set { SetValue(LoadingIconProperty, value); }
	}

	public static readonly DependencyProperty LoadingIconProperty =
		DependencyProperty.Register("LoadingIcon", typeof(IconElement), typeof(LoadingImage), new PropertyMetadata(null));
}
