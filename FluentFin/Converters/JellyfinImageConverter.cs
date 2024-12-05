using FluentFin.Core.Contracts.Services;
using Flurl;
using Jellyfin.Client.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;

namespace FluentFin.Converters;

public partial class JellyfinImageConverter : IValueConverter
{
	public string? BaseUrl { get; set; }
	public ImageInfo_ImageType TypeRequest { get; set; }
	public bool UseSeasonImage { get; set; }



	public object Convert(object value, Type targetType, object parameter, string language)
	{
		if(string.IsNullOrEmpty(BaseUrl))
		{
			BaseUrl = App.GetService<IJellyfinClient>().BaseUrl;
		}

		if(value is BaseItemDto bid)
		{
			if(bid.Name == "Matilda")
			{
				;
			}

			if(bid.ImageTags is null)
			{
				return DependencyProperty.UnsetValue;
			}

			var imageType = bid.ImageTags.Keys.FirstOrDefault(x => x.Equals(TypeRequest.ToString())) ?? bid.ImageTags.Keys.First();
			return new BitmapImage(BaseUrl.AppendPathSegment($"/Items/{bid.Id}/Images/{imageType}").ToUri());
		}

		return DependencyProperty.UnsetValue;
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


	public object Convert(object value, Type targetType, object parameter, string language)
	{
		if (string.IsNullOrEmpty(BaseUrl))
		{
			BaseUrl = App.GetService<IJellyfinClient>().BaseUrl;
		}

		if (value is BaseItemDto bid)
		{
			if (bid.Name == "Matilda")
			{
				;
			}

			if (bid.ImageTags is null)
			{
				return DependencyProperty.UnsetValue;
			}

			var id = bid.Type == BaseItemDto_Type.Episode ? bid.SeasonId : bid.Id;
			return new BitmapImage(BaseUrl.AppendPathSegment($"/Items/{id}/Images/{TypeRequest}").ToUri());
		}

		return DependencyProperty.UnsetValue;
	}

	public object ConvertBack(object value, Type targetType, object parameter, string language)
	{
		throw new NotSupportedException();
	}
}
