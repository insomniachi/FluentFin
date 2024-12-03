using FluentFin.Core.Contracts.Services;

namespace FluentFin.Core.Settings;

public static class SettingKeys
{
	public static Key<ServerSettings> ServerSettings { get; } = new Key<ServerSettings>("ServerSettings", new ServerSettings());
}

