using FluentFin.Core.Contracts.Services;
using Flurl;
using Jellyfin.Client.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace FluentFin.Converters
{
	public partial class JellyfinImageConverter : IValueConverter
	{
		public string? BaseUrl { get; set; }
		public ImageInfo_ImageType Type { get; set; }

		public object Convert(object value, Type targetType, object parameter, string language)
		{
			if(string.IsNullOrEmpty(BaseUrl))
			{
				BaseUrl = App.GetService<IJellyfinClient>().BaseUrl;
			}

			if (value is Guid guid)
			{
				return BaseUrl.AppendPathSegment($"/Items/{guid:N}/Images/{Type}").ToUri();
			}

			if(value is string str)
			{
				return BaseUrl.AppendPathSegment($"/Items/{str}/Images/{Type}").ToUri();
			}

			return DependencyProperty.UnsetValue;
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotSupportedException();
		}
	}
}
