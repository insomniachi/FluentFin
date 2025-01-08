using DeviceId;
using FluentFin.Contracts.Services;
using FluentFin.Core;
using FluentFin.Core.Contracts.Services;
using FluentFin.Core.Settings;
using FluentFin.Core.ViewModels;
using FluentFin.Helpers;
using FluentFin.ViewModels;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace FluentFin.Services;

public class JellyfinAuthenticationService(IJellyfinClient jellyfinClient,
										   ITitleBarViewModel titlebarViewModel,
										   ILocalSettingsService settingsService,
										   [FromKeyedServices(NavigationRegions.InitialSetup)] INavigationService setupNavigationService,
										   ILogger<JellyfinAuthenticationService> logger) : IJellyfinAuthenticationService
{

	public static async Task<PublicSystemInfo?> GetPublicInfo(string url)
	{
		var client = GetClient(url);
		try
		{
			return await client.System.Info.Public.GetAsync();
		}
		catch
		{
			return null;
		}
	}

	public async Task<bool> Authenticate(SavedServer server, SavedUser user)
	{
		var success = await Authenticate(server.GetServerUrl(), user.Username, user.Password.Unprotect(settingsService.GetEntropyBytes()));

		if(success)
		{
			titlebarViewModel.CurrentServer = server;
		}

		return success;
	}

	public async Task<bool> Authenticate(SavedServer server, string username, string password, bool remember)
	{
		var success = await Authenticate(server.GetServerUrl(), username, password);

		if(success)
		{
			titlebarViewModel.CurrentServer = server;

			if (remember)
			{
				var passwordBytes = password.Protect(settingsService.GetEntropyBytes());

				if (server.Users.All(x => x.Username != username && !ByteArraysEqual(x.Password, passwordBytes)))
				{
					var user = new SavedUser
					{
						Username = username,
						Password = passwordBytes,
					};

					server.Users.Add(user);
					settingsService.Save();
				}
			}

			setupNavigationService.NavigateTo(typeof(ShellViewModel).FullName!);
		}

		return success;
	}

	private async Task<bool> Authenticate(string url, string username, string password)
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
				titlebarViewModel.User = auth.User;
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

	private static bool ByteArraysEqual(ReadOnlySpan<byte> a1, ReadOnlySpan<byte> a2)
	{
		return a1.SequenceEqual(a2);
	}
}
