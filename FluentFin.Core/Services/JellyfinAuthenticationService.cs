using DeviceId;
using FluentFin.Core.Contracts.Services;
using FluentFin.Core.Settings;
using FluentFin.Core.ViewModels;
using Flurl;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace FluentFin.Core.Services
{
	public class JellyfinAuthenticationService(IJellyfinClient jellyfinClient,
										       ITitleBarViewModel titleBarViewModel,
										       ILogger<JellyfinAuthenticationService> logger) : IJellyfinAuthenticationService
	{

		public static async Task<PublicSystemInfo?> GetPublicInfo(string url)
		{
			var client = GetClient(url);
			try
			{
				return await client.System.Info.Public.GetAsync();
			}
			catch(Exception ex)
			{
				return null;
			}
		}

		public async Task<bool> Authenticate(string url, string username, string password)
		{

			var client = GetClient(url);

			try
			{
				var auth = await client.Users.AuthenticateByName.PostAsync(new AuthenticateUserByName
				{
					Username = username,
					Pw = password
				});

				if (auth is not null)
				{
					titleBarViewModel.User = auth.User;
					await jellyfinClient.Initialize(url, auth);
				}

				return auth is not null;
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Unhandled exception");
				return false;
			}
		}

		private static JellyfinApiClient GetClient(string url)
		{
			var id = new DeviceIdBuilder().OnWindows(windows => windows.AddWindowsDeviceId()).ToString();
			var settings = new JellyfinSdkSettings();
			settings.SetServerUrl(url);
			settings.Initialize("FluentFin", Assembly.GetEntryAssembly()!.GetName().Version!.ToString(), Environment.MachineName, id);
			return new JellyfinApiClient(new JellyfinRequestAdapter(new JellyfinAuthenticationProvider(settings), settings));
		}
	}
}
