using Jellyfin.Sdk.Generated.Models;

namespace FluentFin.Core
{
#nullable disable
	public static class SessionInfo
	{
		public static string BaseUrl { get; set; }
		public static UserDto CurrentUser { get; set; }
		public static List<PluginInfo> Plugins { get; set; }
		public static string AccessToken { get; set; }
		public static string SessionId { get; set; }

		public static bool CanEditMediaSegments() => CurrentUser?.Policy?.IsAdministrator == true &&
													 Plugins.FirstOrDefault(x => x.Name == "MediaSegments API") is { } &&
													 Plugins.FirstOrDefault(x => x.Name == "Intro Skipper") is { };

		public static bool HasPlaybackReporting() => CurrentUser?.Policy?.IsAdministrator == true && 
													 Plugins.FirstOrDefault(x => x.Name == "Playback Reporting") is { };
	}
#nullable restore
}
