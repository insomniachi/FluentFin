using DeviceId;
using FluentFin.Core.Contracts.Services;
using FluentFin.Core.ViewModels;
using Flurl;
using Flurl.Http;
using Flurl.Http.Configuration;
using Jellyfin.Client;
using Jellyfin.Client.Models;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Text.Json;

namespace FluentFin.Core.Services
{
	public class JellyfinAuthentionService(IJellyfinClient jellyfinClient,
										   ITitleBarViewModel titleBarViewModel,
										   ILogger<JellyfinAuthentionService> logger) : IJellyfinAuthenticationService
	{
		private readonly DefaultJsonSerializer _serializer = new DefaultJsonSerializer(new JsonSerializerOptions
		{
			Converters =
			{
				new NullableGuidConveter(),
			}
		});

		public async Task<bool> Authenticate(string url, string username, string password)
		{

			var id = new DeviceIdBuilder().OnWindows(windows => windows.AddWindowsDeviceId()).ToString();
			var authHeader = $"""MediaBrowser Client="FluentFin", Device="Windows 10/11", DeviceId="{id}", Version="{Assembly.GetEntryAssembly()!.GetName().Version}" """.Trim();

			try
			{
				var auth = await url.AppendPathSegment("/Users/AuthenticateByName")
					.WithHeader("X-Emby-Authorization", authHeader)
					.WithSettings(settings => settings.JsonSerializer = _serializer)
					.PostJsonAsync(new
					{
						Username = username,
						Pw = password
					})
					.ReceiveJson<AuthenticationResult>();

				if (auth is not null)
				{
					titleBarViewModel.User = auth.User;
					jellyfinClient.Initialize(url, auth);
				}

				return auth is not null;
			}
			catch (Exception ex)
			{
				logger.LogError(ex.Message, ex);
				return false;
			}
		}
	}
}
