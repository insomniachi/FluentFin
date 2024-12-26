using FluentFin.Core.Contracts.Services;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace FluentFin.Core.Services;

public class LocalSettingsService : ILocalSettingsService
{
	private readonly JsonObject _settings = [];
	private readonly string _file;
	private readonly JsonSerializerOptions _options;
	public LocalSettingsService(KnownFolders knownFolders)
	{
		_options = new JsonSerializerOptions()
		{
			WriteIndented = true,
			Converters =
			{
				new JsonStringEnumConverter()
			},
			NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
		};

		_file = Path.Combine(knownFolders.ApplicationData, "LocalSettings.json");
		if (File.Exists(_file) && JsonNode.Parse(File.ReadAllText(_file)) is { } jsonNode)
		{
			_settings = jsonNode.AsObject();
		}

	}

	public T ReadSetting<T>(string key, T defaultValue)
	{
		if (!_settings.ContainsKey(key))
		{
			SaveSetting(key, defaultValue);
			return defaultValue;
		}

		return _settings[key].Deserialize<T>(_options) ?? defaultValue;
	}

	public void RemoveSetting(string key)
	{
		_settings.Remove(key);
	}

	public void SaveSetting<T>(string key, T value)
	{
		_settings[key] = JsonNode.Parse(JsonSerializer.Serialize(value, _options));
		File.WriteAllText(_file, _settings.ToJsonString(_options));
	}

	public byte[] GetEntropyBytes()
	{
		return ReadSetting("Entropy", RandomNumberGenerator.GetBytes(20));
	}
}
