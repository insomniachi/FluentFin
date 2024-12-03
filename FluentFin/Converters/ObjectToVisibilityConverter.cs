using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace FluentFin.Converters;

public class ObjectToVisibilityConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, string language)
	{
		if (value is null)
		{
			return Visibility.Collapsed;
		}

		return Visibility.Visible;
	}

	public object ConvertBack(object value, Type targetType, object parameter, string language)
	{
		throw new NotImplementedException();
	}
}
