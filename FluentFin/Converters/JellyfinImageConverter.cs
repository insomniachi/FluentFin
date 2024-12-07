using FluentFin.Core.Contracts.Services;
using Flurl;
using Jellyfin.Sdk.Generated.Models;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;

namespace FluentFin.Converters;

public partial class JellyfinImageConverter : IValueConverter
{
	public string? BaseUrl { get; set; }
	public ImageType TypeRequest { get; set; }
	public bool UseSeasonImage { get; set; }



	public object? Convert(object value, Type targetType, object parameter, string language)
	{
		if(string.IsNullOrEmpty(BaseUrl))
		{
			BaseUrl = App.GetService<IJellyfinClient>().BaseUrl;
		}

		if(value is BaseItemDto bid)
		{
			if(bid is { ImageTags: null or { AdditionalData.Count : 0} })
			{
				return null;
			}

			var imageType = bid.ImageTags.AdditionalData.Keys.FirstOrDefault(x => x.Equals(TypeRequest.ToString())) ?? bid.ImageTags.AdditionalData.Keys.First();
			return new BitmapImage(BaseUrl.AppendPathSegment($"/Items/{bid.Id}/Images/{imageType}").ToUri());
		}

		return null;
	}

	public object ConvertBack(object value, Type targetType, object parameter, string language)
	{
		throw new NotSupportedException();
	}
}

public partial class JellyfinParentImageConverter : IValueConverter
{
	public string? BaseUrl { get; set; }
	public ImageInfo_ImageType TypeRequest { get; set; }


	public object? Convert(object value, Type targetType, object parameter, string language)
	{
		if (string.IsNullOrEmpty(BaseUrl))
		{
			BaseUrl = App.GetService<IJellyfinClient>().BaseUrl;
		}

		if (value is BaseItemDto bid)
		{
			if (bid is { ImageTags: null or { AdditionalData.Count: 0 } })
			{
				return null;
			}

			var id = bid.Type == BaseItemDto_Type.Episode ? bid.SeasonId : bid.Id;
			return new BitmapImage(BaseUrl.AppendPathSegment($"/Items/{id}/Images/{TypeRequest}").ToUri());
		}

		return null;
	}

	public object ConvertBack(object value, Type targetType, object parameter, string language)
	{
		throw new NotSupportedException();
	}
}

public partial class JellyfinImageConverterUniversal : IValueConverter
{
	public string? BaseUrl { get; set; }
	public ImageType TypeRequest { get; set; }


	public object? Convert(object value, Type targetType, object parameter, string language)
	{
		//if (string.IsNullOrEmpty(BaseUrl))
		//{
		//	BaseUrl = App.GetService<IJellyfinClient>().BaseUrl;
		//}

		//if (value is BaseItemDto bid)
		//{
		//	if (bid is { ImageTags: null or { Count: 0 } })
		//	{
		//		return null;
		//	}

		//	var id = bid.Type == BaseItemDto_Type.Episode ? bid.SeasonId : bid.Id;
		//	return new BitmapImage(BaseUrl.AppendPathSegment($"/Items/{id}/Images/{TypeRequest}").ToUri());
		//}

		return null;
	}

	public object ConvertBack(object value, Type targetType, object parameter, string language)
	{
		throw new NotSupportedException();
	}
}
