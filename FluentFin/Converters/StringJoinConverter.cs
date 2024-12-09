using Microsoft.UI.Xaml.Data;

namespace FluentFin.Converters;

public partial class StringJoinConverter : IValueConverter
{
	public string Separator { get; set; } = ",";

	public object? Convert(object value, Type targetType, object parameter, string language)
	{
		if(value is not IEnumerable<object> enumerable)
		{
			return null;
		}

		return string.Join(Separator, enumerable);
	}

	public object ConvertBack(object value, Type targetType, object parameter, string language)
	{
		throw new NotSupportedException();
	}
}
