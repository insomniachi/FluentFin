using FluentFin.Core;
using Flurl;
using Jellyfin.Sdk.Generated.Models;
using Microsoft.UI.Xaml.Media.Imaging;

namespace FluentFin.Converters;

public static class BaseItemDtoConverters
{
	public static string GetCardTitle(this BaseItemDto? dto)
	{
		if (dto is null)
		{
			return string.Empty;
		}

		if (dto.Type == BaseItemDto_Type.Episode)
		{
			return dto.SeriesName ?? "";
		}

		return dto.Name ?? "";
	}

	public static string GetCardSubtitle(this BaseItemDto? dto)
	{
		if (dto is null)
		{
			return string.Empty;
		}

		if (dto.Type == BaseItemDto_Type.Episode)
		{
			return $"S{dto.ParentIndexNumber}:E{dto.IndexNumber} - {dto.Name}";
		}
		if (dto.Type == BaseItemDto_Type.Movie)
		{
			return $"{dto.ProductionYear}";
		}
		if (dto.Type == BaseItemDto_Type.Series)
		{
			return $"{dto.ProductionYear} - {(string.Equals(dto.Status, "Continuing", StringComparison.OrdinalIgnoreCase) ? "Present" : dto.EndDate?.Year)}";
		}

		return "";
	}

	public static string GetSeasonAndEpisodeNumber(this BaseItemDto? dto)
	{
		if (dto is null)
		{
			return string.Empty;
		}

		if (dto.Type is not BaseItemDto_Type.Episode)
		{
			return string.Empty;
		}

		return $"S{dto.ParentIndexNumber}:E{dto.IndexNumber}";
	}

	public static BitmapImage? GetImage(BaseItemPerson personDto, double height)
	{
		return new BitmapImage(SessionInfo.BaseUrl.AppendPathSegment($"/Items/{personDto.Id}/Images/Primary").SetQueryParam("fillHeight", height).ToUri());
	}

	public static BitmapImage? GetImage(VirtualFolderInfo folderInfo)
	{
		return new BitmapImage(SessionInfo.BaseUrl.AppendPathSegment($"/Items/{folderInfo.ItemId}/Images/Primary").ToUri());
	}

	public static BitmapImage? GetImage(BaseItemDto? dto, ImageType imageType, double height)
	{
		if (dto is null)
		{
			return null;
		}

		if (dto.Id is not { } id)
		{
			return null;
		}

		if (dto.ImageTags is null)
		{
			return null;
		}

		var hasRequestTag = dto.ImageTags.AdditionalData.TryGetValue($"{imageType}", out object? requestTag);
		var backdropTag = dto.BackdropImageTags?.FirstOrDefault();
		var parentBackdropTag = dto.ParentBackdropImageTags?.FirstOrDefault();

		var tag = "";
		if (hasRequestTag == true)
		{
			tag = $"{requestTag}";
		}
		else if (!string.IsNullOrEmpty(backdropTag))
		{
			tag = $"{backdropTag}";
		}
		else if (!string.IsNullOrEmpty(parentBackdropTag))
		{
			tag = $"{parentBackdropTag}";
		}

		if (imageType == ImageType.Backdrop && dto.Type is BaseItemDto_Type.Season && dto.ParentId is { } pid)
		{
			id = pid;
		}

		if (imageType == ImageType.Thumb && !hasRequestTag)
		{
			imageType = ImageType.Primary;
			if (dto.ImageTags.AdditionalData.TryGetValue($"{ImageType.Primary}", out var primaryTag))
			{
				tag = $"{primaryTag}";
			}
		}

		else if (imageType == ImageType.Primary && dto.Type == BaseItemDto_Type.Episode)
		{
			if (!string.IsNullOrEmpty(dto.SeriesPrimaryImageTag) && dto.SeriesId is { } seriesId)
			{
				id = seriesId;
				tag = $"{dto.SeriesPrimaryImageTag}";
			}
		}
		else if (imageType == ImageType.Logo && dto.Type == BaseItemDto_Type.Episode)
		{
			if (!string.IsNullOrEmpty(dto.SeriesPrimaryImageTag) && dto.SeriesId is { } seriesId)
			{
				id = seriesId;
				tag = $"{dto.ParentLogoImageTag}";
			}
		}

		var uri = SessionInfo.BaseUrl.AppendPathSegment($"/Items/{id}/Images/{imageType}");

		if (height is { } h)
		{
			uri.SetQueryParam("fillHeight", h);
		}

		if (!string.IsNullOrEmpty(tag))
		{
			uri.SetQueryParam("tag", tag);
		}


		return new BitmapImage(uri.ToUri());
	}
}
