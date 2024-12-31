using Humanizer;
using Microsoft.UI.Xaml.Data;

namespace FluentFin.Converters;

public partial class HumanizeConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, string language)
	{
		if(value is DateTimeOffset offset)
		{
			return offset.Humanize();
		}

		return "";
	}

	public object ConvertBack(object value, Type targetType, object parameter, string language)
	{
		throw new NotSupportedException();
	}
}
