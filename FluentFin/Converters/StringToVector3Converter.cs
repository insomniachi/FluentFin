using System.Numerics;
using System.Windows;
using Microsoft.UI.Xaml.Data;

namespace FluentFin.Converters;

public partial class StringToVector3Converter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, string language)
	{
		if (value != null && value.GetType() == typeof(string))
		{
			var values = ((string)value).Split(" ");
			return new Vector3(float.Parse(values[0]), float.Parse(values[1]), float.Parse(values[2]));
		}

		return DependencyProperty.UnsetValue;
	}

	public object ConvertBack(object value, Type targetType, object parameter, string language)
	{
		throw new NotSupportedException();
	}
}
