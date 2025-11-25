using CommunityToolkit.WinUI;
using FluentFin.Core.Contracts.Services;
using Jellyfin.Sdk.Generated.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;


namespace FluentFin.Controls;

public sealed partial class PlayableMediaItem : Control
{
	[GeneratedDependencyProperty]
	public partial ImageSource? ImageSource { get; set; }

	[GeneratedDependencyProperty]
	public partial BaseItemDto? Model { get; set; }

	[GeneratedDependencyProperty(DefaultValue = "\uE8B9")]
	public partial string Glyph { get; set; }

	private static IJellyfinClient _jellyfinClient = App.GetService<IJellyfinClient>();

	public PlayableMediaItem()
	{
		DefaultStyleKey = typeof(PlayableMediaItem);

		Tapped += PlayableMediaItem_Tapped;
	}

	private void PlayableMediaItem_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
	{
		if (e.OriginalSource is not (Grid or Image))
		{
			return;
		}

		if (Model is null)
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
		if (Model is null)
		{
			return;
		}

		await _jellyfinClient.SetPlayed(Model, !(Model.UserData?.Played ?? false));
	}

	private async void AddToFavoriteButton_Clicked(object sender, RoutedEventArgs e)
	{
		if (Model is null)
		{
			return;
		}

		await _jellyfinClient.SetIsFavorite(Model, !(Model.UserData?.IsFavorite ?? false));
	}
}
