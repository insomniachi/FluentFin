using Microsoft.UI.Xaml.Data;

namespace FluentFin.Converters
{
	public partial class NullableToValueOrDefaultConverter<T> : IValueConverter
		where T : notnull
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
	public partial class NullableBoolToValueOrDefaultConverter : NullableToValueOrDefaultConverter<bool> { }
}
