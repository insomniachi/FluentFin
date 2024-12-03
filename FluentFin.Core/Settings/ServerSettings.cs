using CommunityToolkit.Mvvm.ComponentModel;

namespace FluentFin.Core.Settings;

public partial class ServerSettings : ObservableObject
{
	[ObservableProperty] 
	public partial string ServerUrl { get; set; }
	
	[ObservableProperty]
	public partial string Username { get; set; }
	
	[ObservableProperty] 
	public partial string Password { get; set; }
}