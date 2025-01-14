using Jellyfin.Sdk.Generated.Models;
using Microsoft.UI.Xaml.Data;
namespace FluentFin.Converters;

public partial class JellyfinTitleConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, string language)
	{
		if (value is not BaseItemDto bid)
		{
			return "";
		}

		if (bid.Type == BaseItemDto_Type.Episode)
		{
			return bid.SeriesName ?? "";
		}

		return bid.Name ?? "";
	}

	public object ConvertBack(object value, Type targetType, object parameter, string language)
	{
		throw new NotSupportedException();
	}
}

public partial class JellyfinSubtitleConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, string language)
	{
		if (value is not BaseItemDto bid)
		{
			return "";
		}

		if (bid.Type == BaseItemDto_Type.Episode)
		{
			return $"S{bid.ParentIndexNumber}:E{bid.IndexNumber} - {bid.Name}";
		}
		if (bid.Type == BaseItemDto_Type.Movie)
		{
			return $"{bid.ProductionYear}";
		}
		if (bid.Type == BaseItemDto_Type.Series)
		{
			return $"{bid.ProductionYear} - {(string.Equals(bid.Status, "Continuing", StringComparison.OrdinalIgnoreCase) ? "Present" : bid.EndDate?.Year)}";
		}

		return "";
	}

	public object ConvertBack(object value, Type targetType, object parameter, string language)
	{
		throw new NotSupportedException();
	}
}
