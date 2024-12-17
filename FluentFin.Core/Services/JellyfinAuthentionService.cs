using DeviceId;
using FluentFin.Core.Contracts.Services;
using FluentFin.Core.ViewModels;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace FluentFin.Core.Services
{
	public class JellyfinAuthentionService(IJellyfinClient jellyfinClient,
										   ITitleBarViewModel titleBarViewModel,
										   ILogger<JellyfinAuthentionService> logger) : IJellyfinAuthenticationService
	{

		public async Task<bool> Authenticate(string url, string username, string password)
		{

			var id = new DeviceIdBuilder().OnWindows(windows => windows.AddWindowsDeviceId()).ToString();
			var settings = new JellyfinSdkSettings();
			settings.SetServerUrl(url);
			settings.Initialize("FluentFin", Assembly.GetEntryAssembly()!.GetName().Version!.ToString(), Environment.MachineName, id);
			var client = new JellyfinApiClient(new JellyfinRequestAdapter(new JellyfinAuthenticationProvider(settings), settings));

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
					jellyfinClient.Initialize(url, auth);
				}

				return auth is not null;
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Unhandled exception");
				return false;
			}
		}
	}
}
