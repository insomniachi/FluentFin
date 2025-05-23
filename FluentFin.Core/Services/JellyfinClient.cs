﻿using System.Reflection;
using DeviceId;
using FluentFin.Core.Contracts.Services;
using FluentFin.Core.WebSockets;
using Flurl;
using Jellyfin.Sdk.Generated.Models;
using Microsoft.Extensions.Logging;

namespace FluentFin.Core.Services;

public partial class JellyfinClient(ILogger<JellyfinClient> logger,
									IObserver<IInboundSocketMessage> socketMessageSender,
									IJumpListService jumpListService,
									IDeviceProfileFactory deviceProfileFactory) : IJellyfinClient
{
	private Jellyfin.Sdk.JellyfinApiClient _jellyfinApiClient = null!;
	private string _token = "";
	private Jellyfin.Sdk.JellyfinSdkSettings _settings = null!;
	private string _deviceId = "";
	private PlaybackProgressInfo? _currentItem = null;

	public Guid UserId { get; set; }
	public string BaseUrl { get; set; } = "";

	public async Task Initialize(string baseUrl, AuthenticationResult authResult)
	{
		ArgumentNullException.ThrowIfNull(authResult.User);
		ArgumentNullException.ThrowIfNull(authResult.User.Id);
		ArgumentNullException.ThrowIfNullOrEmpty(authResult.AccessToken);

		SessionInfo.CurrentUser = authResult.User;
		SessionInfo.BaseUrl = baseUrl;
		SessionInfo.AccessToken = authResult.AccessToken;
		SessionInfo.SessionId = authResult.SessionInfo?.Id ?? "";

		UserId = authResult.User.Id.Value;
		BaseUrl = baseUrl;

		_token = authResult.AccessToken;
		_deviceId = new DeviceIdBuilder().OnWindows(windows => windows.AddWindowsDeviceId()).ToString();
		_settings = new Jellyfin.Sdk.JellyfinSdkSettings();
		_settings.Initialize("FluentFin", Assembly.GetEntryAssembly()!.GetName().Version!.ToString()!, Environment.MachineName, _deviceId);
		_settings.SetAccessToken(_token);
		_settings.SetServerUrl(baseUrl);
		_jellyfinApiClient = new Jellyfin.Sdk.JellyfinApiClient(new Jellyfin.Sdk.JellyfinRequestAdapter(new Jellyfin.Sdk.JellyfinAuthenticationProvider(_settings), _settings));

		await _jellyfinApiClient.Sessions.Capabilities.Full.PostAsync(new ClientCapabilitiesDto
		{
			SupportsMediaControl = true,
			PlayableMediaTypes = [MediaType.Video],
			DeviceProfile = deviceProfileFactory.GetDeviceProfile(),
			IconUrl = "https://github.com/insomniachi/FluentFin/raw/master/Installer/base-icon-transparent.png",
			SupportedCommands = [
				GeneralCommandType.DisplayMessage,
			]
		});

		await GetPlugins();
		await StartWebSocketConnection(CancellationToken.None);

		var libaries = new List<BaseItemDto>();
		await foreach (var item in GetUserLibraries())
		{
			libaries.Add(item);
		}
		await jumpListService.Initialize(libaries);
	}

	public async Task<BaseItemDtoQueryResult?> Search(string searchTerm)
	{
		try
		{
			return await _jellyfinApiClient.Items.GetAsync(x =>
			{
				var query = x.QueryParameters;
				query.SearchTerm = searchTerm;
				query.Recursive = true;
				query.Limit = 100;
				query.IncludeItemTypes = [BaseItemKind.Movie, BaseItemKind.Series, BaseItemKind.Person];
			});
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
			return null;
		}
	}

	public async Task Logout()
	{
		try
		{
			await _jellyfinApiClient.Sessions.Logout.PostAsync();
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
		}
	}

	public async Task<int> BitrateTest()
	{
		const int bitrateTestSize = 1024 * 1024 * 3;
		const int chunkSize = 64 * 1024;
		const double safetyRatio = 0.8;
		var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

		int totalBytesRead = 0;
		long startTime = TimeProvider.System.GetTimestamp();
		try
		{
			var stream = await _jellyfinApiClient.Playback.BitrateTest.GetAsync(x => x.QueryParameters.Size = bitrateTestSize, cts.Token);

			if (stream is null)
			{
				return 0;
			}

			var buffer = new byte[chunkSize];

			while (true)
			{
				int bytesRead = await stream.ReadAsync(buffer, cts.Token);
				if (bytesRead == 0)
				{
					break;
				}

				totalBytesRead += bytesRead;
			}
		}
		catch (OperationCanceledException) { }

		double responseTimeSeconds = TimeProvider.System.GetElapsedTime(startTime).TotalSeconds;
		double bytesPerSecond = totalBytesRead / responseTimeSeconds;
		int bitrate = (int)Math.Round(bytesPerSecond * 8 * safetyRatio);
		return bitrate;
	}

	public async Task<SystemInfo?> GetSystemInfo()
	{
		try
		{
			return await _jellyfinApiClient.System.Info.GetAsync();
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
			return null;
		}
	}

	public async Task<List<SessionInfoDto>> GetActiveSessions()
	{
		try
		{
			return await _jellyfinApiClient.Sessions.GetAsync(x => x.QueryParameters.ActiveWithinSeconds = 960) ?? [];
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
			return [];
		}
	}

	public async Task<ActivityLogEntryQueryResult?> GetActivities(DateTimeOffset minDate, bool hasUserId)
	{
		try
		{
			return await _jellyfinApiClient.System.ActivityLog.Entries.GetAsync(x =>
			{
				var query = x.QueryParameters;
				query.StartIndex = 0;
				query.Limit = 7;
				query.MinDate = minDate;
				query.HasUserId = hasUserId;
			});
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
			return null;
		}
	}

	public async Task<ActivityLogEntryQueryResult?> GetAllActivities()
	{
		try
		{
			return await _jellyfinApiClient.System.ActivityLog.Entries.GetAsync(x =>
			{
				var query = x.QueryParameters;
				query.StartIndex = 0;
			});
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
			return null;
		}
	}

	public async Task<List<TaskInfo>> GetScheduledTasks(bool? isEnabled)
	{
		try
		{
			return await _jellyfinApiClient.ScheduledTasks.GetAsync(x => x.QueryParameters.IsEnabled = isEnabled) ?? [];
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
			return [];
		}
	}

	public async Task RunTask(string? taskId)
	{
		try
		{
			await _jellyfinApiClient.ScheduledTasks.Running[taskId].PostAsync();
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
			return;
		}
	}

	public async Task Restart()
	{
		try
		{
			await _jellyfinApiClient.System.Restart.PostAsync();
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
			return;
		}
	}

	public async Task Shutdown()
	{
		try
		{
			await _jellyfinApiClient.System.Shutdown.PostAsync();
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
			return;
		}
	}

	public async Task<ServerConfiguration?> GetConfiguration()
	{
		try
		{
			return await _jellyfinApiClient.System.Configuration.GetAsync();
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
			return null;
		}
	}

	public async Task SaveConfiguration(ServerConfiguration configuration)
	{
		try
		{
			await _jellyfinApiClient.System.Configuration.PostAsync(configuration);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
			return;
		}
	}

	public async Task<EndPointInfo?> EndpointInfo()
	{
		try
		{
			return await _jellyfinApiClient.System.Endpoint.GetAsync();
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
			return null;
		}
	}

	public async Task<List<FileSystemEntryInfo>> GetDirectoryContents(string path)
	{
		try
		{
			return await _jellyfinApiClient.Environment.DirectoryContents.GetAsync(x =>
			{
				var query = x.QueryParameters;
				query.IncludeDirectories = true;
				query.Path = path;
			}) ?? [];
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
			return [];
		}
	}

	public async Task<List<TaskInfo>> GetScheduledTasks()
	{
		try
		{
			return await _jellyfinApiClient.ScheduledTasks.GetAsync(x => x.QueryParameters.IsHidden = false) ?? [];
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
			return [];
		}
	}

	public async Task RunScheduledTask(string id)
	{
		try
		{
			await _jellyfinApiClient.ScheduledTasks.Running[id].PostAsync();
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
			return;
		}
	}

	public async Task SendMessage(string sessionId, MessageCommand command)
	{
		try
		{
			await _jellyfinApiClient.Sessions[sessionId].Message.PostAsync(command);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
			return;
		}
	}

	private Uri AddApiKey(Uri uri)
	{
		return uri.AppendQueryParam("ApiKey", _token).ToUri();
	}
}


