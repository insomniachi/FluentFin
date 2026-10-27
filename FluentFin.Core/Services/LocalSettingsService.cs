using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using FluentFin.Core.Contracts.Services;

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
		try
		{
			if (File.Exists(_file) && JsonNode.Parse(File.ReadAllText(_file)) is { } jsonNode)
			{
				_settings = jsonNode.AsObject();
			}
		}
		catch (Exception ex) when (ex is JsonException or InvalidOperationException)
		{
			// keep the unreadable file for recovery and start with defaults instead of failing every launch
			File.Copy(_file, _file + ".corrupt", true);
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

	public JsonNode? ReadSettingRaw(string key)
	{
		if(!_settings.ContainsKey(key))
		{
			return null;
		}

		return _settings[key];
	}

	public void RemoveSetting(string key)
	{
		_settings.Remove(key);
	}

	public void SaveSetting<T>(string key, T value)
	{
		_settings[key] = JsonNode.Parse(JsonSerializer.Serialize(value, _options));
		// write then rename, so a crash mid-write can't leave a truncated settings file
		var tempFile = _file + ".tmp";
		File.WriteAllText(tempFile, _settings.ToJsonString(_options));
		File.Move(tempFile, _file, true);
	}

	public byte[] GetEntropyBytes()
	{
		return ReadSetting("Entropy", RandomNumberGenerator.GetBytes(20));
	}
}
