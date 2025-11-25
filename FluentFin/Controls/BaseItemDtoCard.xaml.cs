using CommunityToolkit.WinUI;
using FluentFin.Core.Contracts.Services;
using FluentFin.Core.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;


namespace FluentFin.Controls;

public sealed partial class BaseItemDtoCard : UserControl
{
	[GeneratedDependencyProperty]
	public partial ImageSource? ImageSource { get; set; }

	[GeneratedDependencyProperty]
	public partial BaseItemViewModel? Model { get; set; }

	[GeneratedDependencyProperty]
	public partial WriteableBitmap? BlurHashImageSource { get; set; }

	[GeneratedDependencyProperty(DefaultValue = "\uE8B9")]
	public partial string Glyph { get; set; }

	[GeneratedDependencyProperty]
	public partial IJellyfinClient? JellyfinClient { get; set; }

	public BaseItemDtoCard()
	{
		InitializeComponent();
	}

	private void ImageCanvas_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
	{
		if (e.OriginalSource is not (Grid or Image))
		{
			return;
		}

		if (Model is null)
		{
			return;
		}

		App.Commands.DisplayDto(Model.Dto);

		e.Handled = true;
	}

	private async void MarkWatchedButton_Clicked(object sender, RoutedEventArgs e)
	{
		if (Model is null || JellyfinClient is null)
		{
			return;
		}

		await JellyfinClient.SetPlayed(Model.Dto, !(Model.UserData?.Played ?? false));
	}

	private async void AddToFavoriteButton_Clicked(object sender, RoutedEventArgs e)
	{
		if (Model is null || JellyfinClient is null)
		{
			return;
		}

		await JellyfinClient.SetIsFavorite(Model.Dto, !(Model.UserData?.IsFavorite ?? false));
	}
}
