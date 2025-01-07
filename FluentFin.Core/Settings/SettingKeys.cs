using FluentFin.Core.Contracts.Services;

namespace FluentFin.Core.Settings;

public static class SettingKeys
{
	public static Key<ServerSettings> ServerSettings { get; } = new Key<ServerSettings>("ServerSettings", new ServerSettings());
	public static Key<List<SavedServer>> Servers { get; } = new Key<List<SavedServer>>("Servers", []);
	public static Key<List<SavedUser>> Users { get; } = new Key<List<SavedUser>>("Users", []);
}

