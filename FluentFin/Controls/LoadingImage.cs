using DevWinUI;
using Microsoft.UI.Xaml;

namespace FluentFin.Controls;

public partial class LoadingImage : ImageEx
{
	public LoadingImage()
	{
		DefaultStyleKey = typeof(LoadingImage);
	}

	public string Glyph
	{
		get { return (string)GetValue(GlyphProperty); }
		set { SetValue(GlyphProperty, value); }
	}

	public static readonly DependencyProperty GlyphProperty =
		DependencyProperty.Register("Glyph", typeof(string), typeof(LoadingImage), new PropertyMetadata("\uE8B9"));
}
