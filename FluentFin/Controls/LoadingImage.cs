using CommunityToolkit.WinUI;
using DevWinUI;

namespace FluentFin.Controls;

public partial class LoadingImage : ImageEx
{
	[GeneratedDependencyProperty(DefaultValue = "\uE8B9")]
	public partial string Glyph { get; set; }

	public LoadingImage()
	{
		DefaultStyleKey = typeof(LoadingImage);
	}
}
