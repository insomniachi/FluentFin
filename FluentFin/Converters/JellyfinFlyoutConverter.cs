using Jellyfin.Sdk.Generated.Models;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

namespace FluentFin.Converters;

public partial class JellyfinFlyoutConverter : IValueConverter
{
	public object? Convert(object value, Type targetType, object parameter, string language)
	{
		if(value is not BaseItemDto dto)
		{
			return null;
		}

		if(dto.Type is not { } type)
		{
			return null;
		}

		var flyout = new MenuBarItemFlyout();

		foreach (var item in GetPlayItems(dto, type))
		{
			flyout.Items.Add(item);
		}

		foreach (var item in GetDtoItems(type))
		{
			flyout.Items.Add(item);
		}

		foreach (var item in GetMetaDataItems(type))
		{
			flyout.Items.Add(item);
		}

		return flyout;
	}

	public object ConvertBack(object value, Type targetType, object parameter, string language)
	{
		throw new NotSupportedException();
	}

	private static IEnumerable<MenuFlyoutItemBase> GetMetaDataItems(BaseItemDto_Type type)
	{
		yield return new MenuFlyoutItem
		{
			Text = "Edit Metadata",
			Icon = new SymbolIcon { Symbol = Symbol.Edit }
		};
		yield return new MenuFlyoutItem
		{
			Text = "Edit Images",
			Icon = new FontIcon { Glyph = "\uEE71" }
		};

		if(type is BaseItemDto_Type.Movie or BaseItemDto_Type.Episode)
		{
			yield return new MenuFlyoutItem
			{
				Text = "Edit Subtitles",
				Icon = new SymbolIcon { Symbol = Symbol.ClosedCaption }
			};
		}

		if(type is not BaseItemDto_Type.CollectionFolder)
		{
			yield return new MenuFlyoutItem
			{
				Text = "Identify",
				Icon = new SymbolIcon { Symbol = Symbol.Edit }
			};
		}

		if(type is BaseItemDto_Type.Movie or BaseItemDto_Type.Episode)
		{
			yield return new MenuFlyoutItem
			{
				Text = "Media Info",
				Icon = new SymbolIcon { Symbol = Symbol.MusicInfo }
			};
		}

		yield return new MenuFlyoutItem
		{
			Text = "Refresh metadata",
			Icon = new SymbolIcon { Symbol = Symbol.Refresh }
		};
	}

	private static IEnumerable<MenuFlyoutItemBase> GetPlayItems(BaseItemDto dto, BaseItemDto_Type type)
	{
		if(type is BaseItemDto_Type.Movie or BaseItemDto_Type.Season or BaseItemDto_Type.Series or BaseItemDto_Type.Episode)
		{
			yield return new MenuFlyoutItem
			{
				Text = "Play",
				Icon = new SymbolIcon { Symbol = Symbol.Play },
				Command = App.Commands.PlayDtoCommand,
				CommandParameter = dto
			};
		}

		if(type is BaseItemDto_Type.Movie or BaseItemDto_Type.Episode)
		{
			yield return new MenuFlyoutItem
			{
				Text = "Play all from here",
				Icon = new SymbolIcon { Symbol = Symbol.Play }
			};
		}

		if(type is BaseItemDto_Type.Season or BaseItemDto_Type.Series or BaseItemDto_Type.CollectionFolder)
		{
			yield return new MenuFlyoutItem
			{
				Text = "Shuffle",
				Icon = new SymbolIcon { Symbol = Symbol.Shuffle }
			};
		}

		yield return new MenuFlyoutSeparator();
	}

	private static IEnumerable<MenuFlyoutItemBase> GetDtoItems(BaseItemDto_Type type)
	{

		yield return new MenuFlyoutItem
		{
			Text = "Add to playlist",
			Icon = new FontIcon { Glyph = "\uECC8" }
		};

		if (type is not BaseItemDto_Type.CollectionFolder)
		{
			yield return new MenuFlyoutItem
			{
				Text = "Add to collection",
				Icon = new SymbolIcon { Symbol = Symbol.Play }
			};
		}

		if(type is BaseItemDto_Type.Movie)
		{
			yield return new MenuFlyoutItem
			{
				Text = "Download",
				Icon = new SymbolIcon { Symbol = Symbol.Download }
			};
			yield return new MenuFlyoutItem
			{
				Text = "Copy Stream URL",
				Icon = new SymbolIcon { Symbol = Symbol.Copy }
			};
			yield return new MenuFlyoutItem
			{
				Text = "Delete Media",
				Icon = new SymbolIcon { Symbol = Symbol.Delete }
			};
		}

		if(type is BaseItemDto_Type.Series or BaseItemDto_Type.Season)
		{
			yield return new MenuFlyoutItem
			{
				Text = "Download All",
				Icon = new SymbolIcon { Symbol = Symbol.Download }
			};
			yield return new MenuFlyoutItem
			{
				Text = "Delete Series",
				Icon = new SymbolIcon { Symbol = Symbol.Delete }
			};
		}

		yield return new MenuFlyoutSeparator();
	}
}
