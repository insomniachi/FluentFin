namespace FluentFin.Core.Settings;

public partial class ServerSettings
{
	public string ServerUrl { get; set; } = "";
	
	public string Username { get; set; } = "";
	
	public byte[] Password { get; set; } = [];
}