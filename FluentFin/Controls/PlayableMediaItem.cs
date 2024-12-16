using FluentFin.Core.Contracts.Services;
using Jellyfin.Sdk.Generated.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;


namespace FluentFin.Controls;

public sealed partial class PlayableMediaItem : Control
{
	public ImageSource ImageSource
	{
		get { return (ImageSource)GetValue(ImageSourceProperty); }
		set { SetValue(ImageSourceProperty, value); }
	}

	public BaseItemDto Model
	{
		get { return (BaseItemDto)GetValue(ModelProperty); }
		set { SetValue(ModelProperty, value); }
	}

	public Symbol LoadingSymbol
	{
		get { return (Symbol)GetValue(LoadingSymbolProperty); }
		set { SetValue(LoadingSymbolProperty, value); }
	}

	public static readonly DependencyProperty LoadingSymbolProperty =
		DependencyProperty.Register("LoadingSymbol", typeof(Symbol), typeof(PlayableMediaItem), new PropertyMetadata(Symbol.Pictures));

	public static readonly DependencyProperty ModelProperty =
		DependencyProperty.Register("Model", typeof(BaseItemDto), typeof(PlayableMediaItem), new PropertyMetadata(null));

	public static readonly DependencyProperty ImageSourceProperty =
		DependencyProperty.Register(nameof(ImageSource), typeof(ImageSource), typeof(PlayableMediaItem), new PropertyMetadata(null));

	private static IJellyfinClient _jellyfinClient = App.GetService<IJellyfinClient>();

	public PlayableMediaItem()
	{
		DefaultStyleKey = typeof(PlayableMediaItem);

		Tapped += PlayableMediaItem_Tapped;
	}

	private void PlayableMediaItem_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
	{
		if(e.OriginalSource is not (Grid or Image))
		{
			return;
		}

		App.Commands.DisplayDto(Model);

		e.Handled = true;
	}

	protected override void OnApplyTemplate()
	{
		var markWatchedButton = (ToggleButton)GetTemplateChild("MarkWatchedButton");
		var addToFavoriteButton = (ToggleButton)GetTemplateChild("AddToFavoriteButton");
		var playButton = (Button)GetTemplateChild("PlayButton");
		playButton.Command = App.Commands.PlayDtoCommand;
		playButton.CommandParameter = Model;

		markWatchedButton.Checked += MarkWatchedButton_Clicked;
		markWatchedButton.Unchecked += MarkWatchedButton_Clicked;
		addToFavoriteButton.Checked += AddToFavoriteButton_Clicked;
		addToFavoriteButton.Unchecked += AddToFavoriteButton_Clicked;

		base.OnApplyTemplate();
	}


	private async void MarkWatchedButton_Clicked(object sender, RoutedEventArgs e)
	{
		var response = await _jellyfinClient.ToggleMarkAsWatched(Model);

		if(response is null)
		{
			return;
		}

		Model.UserData = response;
	}

	private async void AddToFavoriteButton_Clicked(object sender, RoutedEventArgs e)
	{
		var response = await _jellyfinClient.ToggleMarkAsFavorite(Model);

		if (response is null)
		{
			return;
		}

		Model.UserData = response;
	}
}
