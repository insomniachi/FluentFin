using System.Text.Json;

namespace Jellyfin.Client;

public class NullableGuidConveter : JsonConverter<Guid?>
{
	public override Guid? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var guid = reader.GetString();
		if(Guid.TryParse(guid, out var g))
		{
			return g;
		}

		return null;
	}

	public override void Write(Utf8JsonWriter writer, Guid? value, JsonSerializerOptions options)
	{
		if(value is not null)
		{
			writer.WriteStringValue(value.Value.ToString("N"));
		}
	}
}