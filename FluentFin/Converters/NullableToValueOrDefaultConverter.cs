using Microsoft.UI.Xaml.Data;
using System.Numerics;

namespace FluentFin.Converters
{
	public partial class NullableToValueOrDefaultConverter<T> : IValueConverter
		where T : INumber<T>
	{
		public T Default { get; set; } = default!;

		public object Convert(object value, Type targetType, object parameter, string language)
		{
			return value switch
			{
				T number => number,
				_ => Default
			};

		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new NotSupportedException();
		}
	}

	public partial class NullableDoubleToValueOrDefaultConverter : NullableToValueOrDefaultConverter<double> { }
	public partial class NullableIntToValueOrDefaultConverter : NullableToValueOrDefaultConverter<int> { }
}
