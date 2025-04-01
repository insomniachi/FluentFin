using FluentFin.Core;
using FluentFin.Core.Contracts.Services;
using FluentFin.Core.Settings;
using Jellyfin.Sdk.Generated.Models;

namespace FluentFin.Services
{
	public class DeviceProfileFactory(ISettings settings) : IDeviceProfileFactory
	{
		public static async Task Initialize()
		{
			await WmpDeviceProfileManager.InitializeAsync();
		}

		public DeviceProfile GetDeviceProfile() => GetDeviceProfile(settings.MediaPlayer);

		private static DeviceProfile GetDeviceProfile(MediaPlayerType type)
		{
			return type switch
			{
				MediaPlayerType.Flyleaf => DeviceProfiles.Flyleaf,
				MediaPlayerType.WindowsMediaPlayer => WmpDeviceProfileManager.Profile,
				_ => DeviceProfiles.Flyleaf,
			};
		}
	}
}
