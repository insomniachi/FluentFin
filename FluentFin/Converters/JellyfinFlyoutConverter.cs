using FluentFin.Core;
using Jellyfin.Sdk.Generated.Models;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;

namespace FluentFin.Converters;

public partial class JellyfinFlyoutConverter : IValueConverter
{
	public object? Convert(object value, Type targetType, object parameter, string language)
	{
		if (value is not BaseItemDto dto)
		{
			return null;
		}

		if (dto.Type is not { } type)
		{
			return null;
		}

		var flyout = new MenuBarItemFlyout();

		foreach (var item in GetPlayItems(dto, type))
		{
			flyout.Items.Add(item);
		}

		foreach (var item in GetDtoItems(dto, type))
		{
			flyout.Items.Add(item);
		}

		foreach (var item in GetMetaDataItems(dto, type))
		{
			flyout.Items.Add(item);
		}

		return flyout;
	}

	public object ConvertBack(object value, Type targetType, object parameter, string language)
	{
		throw new NotSupportedException();
	}

	private static IEnumerable<MenuFlyoutItemBase> GetMetaDataItems(BaseItemDto dto, BaseItemDto_Type type)
	{
		yield return new MenuFlyoutItem
		{
			Text = "Edit Metadata",
			Icon = new SymbolIcon { Symbol = Symbol.Edit },
			Command = App.Dialogs.EditMetadataDialogCommand,
			CommandParameter = dto
		};

		yield return new MenuFlyoutItem
		{
			Text = "Edit Images",
			Icon = new FontIcon { Glyph = "\uEE71" },
			Command = App.Dialogs.EditImagesDialogCommand,
			CommandParameter = dto
		};

		if (SessionInfo.CanEditMediaSegments())
		{
			yield return new MenuFlyoutItem
			{
				Text = "Edit Segments",
				Icon = new FontIcon { Glyph = "\uE7A8" },
				Command = App.Commands.NavigateToSegmentsEditorCommand,
				CommandParameter = dto
			};
		}

		if (type is BaseItemDto_Type.Movie or BaseItemDto_Type.Episode)
		{
			yield return new MenuFlyoutItem
			{
				Text = "Edit Subtitles",
				Icon = new SymbolIcon { Symbol = Symbol.ClosedCaption },
				Command = App.Dialogs.EditSubtitlesDialogCommand,
				CommandParameter = dto
			};
		}

		if (type is not BaseItemDto_Type.CollectionFolder)
		{
			yield return new MenuFlyoutItem
			{
				Text = "Identify",
				Icon = new SymbolIcon { Symbol = Symbol.Edit },
				Command = App.Dialogs.IdentifyDialogCommand,
				CommandParameter = dto,
			};
		}

		if (type is BaseItemDto_Type.Movie or BaseItemDto_Type.Episode)
		{
			yield return new MenuFlyoutItem
			{
				Text = "Media Info",
				Icon = new SymbolIcon { Symbol = Symbol.MusicInfo },
				Command = App.Dialogs.MediaInfoDialogCommand,
				CommandParameter = dto,
			};
		}

		yield return new MenuFlyoutItem
		{
			Text = "Refresh metadata",
			Icon = new SymbolIcon { Symbol = Symbol.Refresh },
			Command = App.Dialogs.RefreshMetadataDialogCommand,
			CommandParameter = dto.Id,
		};
	}

	private static IEnumerable<MenuFlyoutItemBase> GetPlayItems(BaseItemDto dto, BaseItemDto_Type type)
	{
		if (type is BaseItemDto_Type.Movie or BaseItemDto_Type.Season or BaseItemDto_Type.Series or BaseItemDto_Type.Episode)
		{
			yield return new MenuFlyoutItem
			{
				Text = "Play",
				Icon = new SymbolIcon { Symbol = Symbol.Play },
				Command = App.Commands.PlayDtoCommand,
				CommandParameter = dto
			};

            yield return new MenuFlyoutItem
            {
                Text = "Play On",
                Icon = new FontIcon { Glyph = "\uE8AF" },
                Command = App.Dialogs.PlayOnSessionCommand,
                CommandParameter = dto
            };
        }

		if (type is BaseItemDto_Type.Movie or BaseItemDto_Type.Episode)
		{
			yield return new MenuFlyoutItem
			{
				Text = "Play all from here",
				Icon = new SymbolIcon { Symbol = Symbol.Play }
			};
		}

		if (type is BaseItemDto_Type.Season or BaseItemDto_Type.Series or BaseItemDto_Type.CollectionFolder)
		{
			yield return new MenuFlyoutItem
			{
				Text = "Shuffle",
				Icon = new SymbolIcon { Symbol = Symbol.Shuffle }
			};
		}

		yield return new MenuFlyoutSeparator();
	}

	private static IEnumerable<MenuFlyoutItemBase> GetDtoItems(BaseItemDto dto, BaseItemDto_Type type)
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

		if (type is BaseItemDto_Type.Movie)
		{
			yield return new MenuFlyoutItem
			{
				Text = "Copy Download URL",
				Icon = new SymbolIcon { Symbol = Symbol.Copy },
				Command = App.Dialogs.CopyUrlToClipboardCommand,
				CommandParameter = dto,
			};


			if(SessionInfo.CurrentUser?.Policy?.IsAdministrator == true)
			{
				yield return new MenuFlyoutItem
				{
					Text = "Delete Media",
					Icon = new SymbolIcon { Symbol = Symbol.Delete },
					Command = App.Commands.DeleteItemCommand,
					CommandParameter = dto,
				};
			}
		}

		if (type is BaseItemDto_Type.Series or BaseItemDto_Type.Season)
		{
			if (SessionInfo.CurrentUser?.Policy?.IsAdministrator == true)
			{
				yield return new MenuFlyoutItem
				{
					Text = "Delete Series",
					Icon = new SymbolIcon { Symbol = Symbol.Delete },
					Command = App.Commands.DeleteItemCommand,
					CommandParameter = dto,
				};
			}
		}

		yield return new MenuFlyoutSeparator();
	}
}
