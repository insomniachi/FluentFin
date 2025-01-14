using FluentFin.Core.Contracts.Services;
using Flurl;
using Jellyfin.Sdk.Generated.Models;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;

namespace FluentFin.Converters;

public partial class JellyfinImageConverter : IValueConverter
{
	public ImageType TypeRequest { get; set; } = ImageType.Primary;
	public double ImageHeight { get; set; } = 300;

	public object? Convert(object value, Type targetType, object parameter, string language)
	{
		if(value is not BaseItemDto { } dto)
		{
			return null;
		}

		return BaseItemDtoConverters.GetImage(dto, TypeRequest, ImageHeight);
	}

	public object ConvertBack(object value, Type targetType, object parameter, string language)
	{
		throw new NotSupportedException();
	}
}

public partial class JellyfinVirtualFolderImageConverter : IValueConverter
{
	private static IJellyfinClient _jellyfinClient = App.GetService<IJellyfinClient>();

	public object? Convert(object value, Type targetType, object parameter, string language)
	{
		if (value is not VirtualFolderInfo { } info)
		{
			return null;
		}
		var uri = _jellyfinClient.BaseUrl.AppendPathSegment($"/Items/{info.ItemId}/Images/Primary").ToUri();

		if (uri is null)
		{
			return null;
		}

		return new BitmapImage(uri);
	}

	public object ConvertBack(object value, Type targetType, object parameter, string language)
	{
		throw new NotSupportedException();
	}
}

public partial class JellyfinPersonImageConverter : IValueConverter
{
	public double ImageHeight { get; set; } = 300;

	public object? Convert(object value, Type targetType, object parameter, string language)
	{
		if(value is not BaseItemPerson { } dto)
		{
			return null;
		}

		return BaseItemDtoConverters.GetImage(dto, ImageHeight);
	}

	public object ConvertBack(object value, Type targetType, object parameter, string language)
	{
		throw new NotSupportedException();
	}
}
