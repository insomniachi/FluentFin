namespace FluentFin.Core.Contracts.Services;

public interface ILocalSettingsService
{
	T ReadSetting<T>(string key, T defaultValue);
	void SaveSetting<T>(string key, T value);
	void RemoveSetting(string key);
	byte[] GetEntropyBytes();
	void Save();
}

public static class LocalSettingsServiceExtensions
{
	public static T ReadSetting<T>(this ILocalSettingsService service, Key<T> key)
	{
		return service.ReadSetting(key.Name, key.Default.Value);
	}

	public static T? ReadSetting<T>(this ILocalSettingsService service, Key<T> key, T defaultValue)
	{
		return service.ReadSetting(key.Name, defaultValue);
	}

	public static void SaveSetting<T>(this ILocalSettingsService service, Key<T> key, T value)
	{
		service.SaveSetting(key.Name, value);
	}
}

public interface IKey
{
	string Name { get; }
}

public class Key<T> : IKey
{
	public string Name { get; }
	public Lazy<T> Default { get; }

	public Key(string name, T defaultValue)
	{
		Name = name;
		Default = new Lazy<T>(defaultValue);
	}

	public Key(string name, Func<T> defaultValueFactory)
	{
		Name = name;
		Default = new Lazy<T>(defaultValueFactory);
	}

	public Key(string name)
	{
		Name = name;
		Default = new Lazy<T>();
	}
}
