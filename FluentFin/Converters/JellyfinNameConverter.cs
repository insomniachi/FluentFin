using Jellyfin.Client.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
namespace FluentFin.Converters;

public partial class JellyfinNameConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, string language)
	{
		if (value is not BaseItemDto bid)
		{
			return DependencyProperty.UnsetValue;
		}

		if(bid.Type == BaseItemDto_Type.Episode)
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
