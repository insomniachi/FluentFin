namespace FluentFin.Core.Settings;

public partial class ServerSettings
{
	public string ServerUrl { get; set; } = "";
	
	public string Username { get; set; } = "";
	
	public byte[] Password { get; set; } = [];
}

public class SavedServer
{
	public string DisplayName { get; set; } = "";
	public string PublicUrl { get; set; } = "";
	public string LocalUrl { get; set; } = "";
	public List<string> LocalNetworkNames { get; set; } = [];
	public string Id { get; set; } = "";
}

public class SavedUser
{
	public string Username { get; set; } = "";
	public byte[] Password { get; set; } = [];
	public string ServerId { get; set; } = "";
}