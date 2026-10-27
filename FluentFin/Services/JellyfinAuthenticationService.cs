using System.Reflection;
using System.Security.Authentication;
using DeviceId;
using FluentFin.Contracts.Services;
using FluentFin.Core;
using FluentFin.Core.Contracts.Services;
using FluentFin.Core.Services;
using FluentFin.Core.Settings;
using FluentFin.Core.ViewModels;
using FluentFin.Helpers;
using FluentFin.ViewModels;
using Jellyfin.Sdk;
using Jellyfin.Sdk.Generated.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Kiota.Abstractions;

namespace FluentFin.Services;

public class JellyfinAuthenticationService(IJellyfinClient jellyfinClient,
										   ITitleBarViewModel titleBarViewModel,
										   ILocalSettingsService settingsService,
										   [FromKeyedServices(NavigationRegions.InitialSetup)] INavigationService setupNavigationService,
										   ILogger<JellyfinAuthenticationService> logger) : IJellyfinAuthenticationService
{

	public static Uri GetSplashScreen(string url)
	{
		var client = GetClient(url);

		return client.Branding.Splashscreen.ToGetRequestInformation(x =>
		{
			var query = x.QueryParameters;
			query.Format = Jellyfin.Sdk.Generated.Branding.Splashscreen.ImageFormat.Jpg;
		}).URI;
	}

	public static async Task<(PublicSystemInfo? Info, string? Error)> GetPublicInfo(string url)
	{
		try
		{
			var client = GetClient(url);
			return (await client.System.Info.Public.GetAsync(), null);
		}
		catch (Exception ex)
		{
			return (null, DescribeFailure(ex, url));
		}
	}

	private static string DescribeFailure(Exception ex, string url) => ex switch
	{
		ApiException { ResponseStatusCode: 401 } => "Incorrect username or password.",
		HttpRequestException { InnerException: AuthenticationException } =>
			$"The HTTPS certificate of {url} isn't trusted by Windows (for example a self-signed certificate). " +
			"Use the server's http:// address, or add its certificate to the Windows certificate store.",
		HttpRequestException => $"Unable to reach {url}: {ex.Message}",
		_ => ex.Message
	};

	public async Task<QuickConnectResult?> GetQuickConnectCode(SavedServer server)
	{
		var client = GetClient(server.GetServerUrl());
		try
		{
			return await client.QuickConnect.Initiate.PostAsync();
		}
		catch
		{
			return null;
		}
	}

	public async Task<QuickConnectResult?> CheckQuickConnectStatus(SavedServer server, QuickConnectResult result)
	{
		var client = GetClient(server.GetServerUrl());
		try
		{
			return await client.QuickConnect.Connect.GetAsync(x =>
			{
				x.QueryParameters.Secret = result.Secret;
			});
		}
		catch
		{
			return null;
		}
	}

	public async Task<bool> Authenticate(SavedServer server, QuickConnectResult result)
	{
		var url = server.GetServerUrl();
		var client = GetClient(url);
		try
		{
			var auth = await client.Users.AuthenticateWithQuickConnect.PostAsync(new QuickConnectDto
			{
				Secret = result.Secret,
			});

			if (auth is not null)
			{
				titleBarViewModel.User = auth.User;
				SessionInfo.CurrentUserCrendentials = null;
				await jellyfinClient.Initialize(url, auth);
				setupNavigationService.NavigateTo<ShellViewModel>();
			}

			return auth is not null;
		}
		catch
		{
			return false;
		}
	}

	public async Task<bool> Authenticate(SavedServer server, SavedUser user)
	{
		var success = await Authenticate(server.GetServerUrl(), user.Username, user.Password.Unprotect(settingsService.GetEntropyBytes())) is null;

		if (success)
		{
			titleBarViewModel.CurrentServer = server;
			SessionInfo.CurrentUserCrendentials = user;
		}

		return success;
	}

	public async Task<string?> Authenticate(SavedServer server, string username, string password, bool remember)
	{
		var error = await Authenticate(server.GetServerUrl(), username, password);

		if (error is null)
		{
			titleBarViewModel.CurrentServer = server;

			var user = new SavedUser
			{
				Username = username,
				Password = password.Protect(settingsService.GetEntropyBytes()),
			};
			SessionInfo.CurrentUserCrendentials = user;

			if (remember)
			{
				// replace rather than mutate so the Users collection change gets saved
				var existing = server.Users.FirstOrDefault(x => x.Username == username);
				if (existing is null)
				{
					server.Users.Add(user);
				}
				else
				{
					server.Users[server.Users.IndexOf(existing)] = user;
				}
			}

			setupNavigationService.NavigateTo<ShellViewModel>();
		}

		return error;
	}

	/// <returns><c>null</c> on success, otherwise a message describing why sign-in failed.</returns>
	private async Task<string?> Authenticate(string url, string username, string password)
	{
		try
		{
			var client = GetClient(url);
			var auth = await client.Users.AuthenticateByName.PostAsync(new AuthenticateUserByName
			{
				Username = username,
				Pw = password
			});

			if (auth is null)
			{
				return "The server returned no session.";
			}

			titleBarViewModel.User = auth.User;
			await jellyfinClient.Initialize(url, auth);
			return null;
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Sign-in to {Url} failed", url);
			return DescribeFailure(ex, url);
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
