using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FluentFin.Core.WebSockets;


internal sealed class JsonGuidConverter : JsonConverter<Guid>
{
	/// <inheritdoc />
	public override Guid Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		=> reader.TokenType == JsonTokenType.Null
			? Guid.Empty
			: ReadInternal(ref reader);

	/// <inheritdoc />
	public override void Write(Utf8JsonWriter writer, Guid value, JsonSerializerOptions options)
		=> WriteInternal(writer, value);

	internal static Guid ReadInternal(ref Utf8JsonReader reader)
		=> Guid.Parse(reader.GetString()!); // null got handled higher up the call stack

	internal static void WriteInternal(Utf8JsonWriter writer, Guid value)
		=> writer.WriteStringValue(value.ToString("N", CultureInfo.InvariantCulture));
}

internal sealed class JsonNullableGuidConverter : JsonConverter<Guid?>
{
	/// <inheritdoc />
	public override Guid? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		=> JsonGuidConverter.ReadInternal(ref reader);

	/// <inheritdoc />
	public override void Write(Utf8JsonWriter writer, Guid? value, JsonSerializerOptions options)
	{
		// null got handled higher up the call stack
		var val = value!.Value;
		if (val.Equals(default))
		{
			writer.WriteNullValue();
		}
		else
		{
			JsonGuidConverter.WriteInternal(writer, val);
		}
	}
}
