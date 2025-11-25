using System.Collections.ObjectModel;

namespace FluentFin.Core.Settings;

public class SavedServer
{
	public string DisplayName { get; set; } = "";
	public string PublicUrl { get; set; } = "";
	public string LocalUrl { get; set; } = "";
	public List<string> LocalNetworkNames { get; set; } = [];
	public string Id { get; set; } = "";
	public ObservableCollection<SavedUser> Users { get; set; } = [];
}

public class SavedUser
{
	public string Username { get; set; } = "";
	public byte[] Password { get; set; } = [];
}

public record CustomNavigationViewItem
{
	public string Name { get; set; } = "";
	public string Key { get; set; } = "";
	public Guid? Parameter { get; set; }
	public string? Glyph { get; set; } = "";
	public List<CommandModel> Commands { get; set; } = [];
	public bool Persistent { get; set; }
}

public record CommandModel
{
	public string Name { get; set; } = "";
	public string DisplayName { get; set; } = "";
	public string? Glyph { get; set; }
}