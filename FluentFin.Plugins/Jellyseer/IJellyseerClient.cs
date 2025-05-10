using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentFin.Core;
using FluentFin.Core.Contracts.Services;
using FluentFin.Plugins.Jellyseer.Models;
using FluentFin.Plugins.Jellyseer.Request;
using Flurl;
using Microsoft.Kiota.Abstractions;
using Microsoft.Kiota.Abstractions.Authentication;
using Microsoft.Kiota.Http.HttpClientLibrary;

namespace FluentFin.Plugins.Jellyseer;

public interface IJellyseerClient
{
	string BaseUrl { get; }
	Task<bool> Login();
	Task<RequestGetResponse?> GetRequests();
	Task<MediaRequest?> ApproveRequest(MediaRequest request);
	Task<MediaRequest?> DeclineRequest(MediaRequest request);
	Task<MovieDetails?> GetMovieDetails(MediaRequest request);
	Task<TvDetails?> GetSeriesDetails(MediaRequest request);
}

public class NullAuthenticationProvider : IAuthenticationProvider
{
	public Task AuthenticateRequestAsync(RequestInformation request, Dictionary<string, object>? additionalAuthenticationContext = null, CancellationToken cancellationToken = default)
	{
		return Task.CompletedTask;
	}
}

public class JellyseerClient(ILocalSettingsService settingsService) : IJellyseerClient
{
	private ApiClient _apiClient = null!;

	public string BaseUrl { get; private set; } = "";

	public async Task<bool> Login()
	{
		var options = JellyseerOptions.Current;

		if (!options.ServerMapping.TryGetValue(SessionInfo.ServerId, out string? url))
		{
			return false;
		}

		if (_apiClient is not null)
		{
			return true;
		}

		BaseUrl = url;

		_apiClient = new ApiClient(new HttpClientRequestAdapter(new NullAuthenticationProvider())
		{
			BaseUrl = url.AppendPathSegment("/api/v1/")
		});

		await _apiClient.Auth.Jellyfin.PostAsync(new Auth.Jellyfin.JellyfinPostRequestBody
		{
			Username = SessionInfo.CurrentUserCrendentials.Username,
			Password = SessionInfo.CurrentUserCrendentials.Password.Unprotect(settingsService.GetEntropyBytes())
		});

		return true;
	}

	public async Task<RequestGetResponse?> GetRequests()
	{
		var result = await _apiClient.Request.GetAsRequestGetResponseAsync();
		return result;
	}

	public async Task<MediaRequest?> ApproveRequest(MediaRequest request)
	{
		return await _apiClient.Request[$"{request.Id}"]["approve"].PostAsync();
	}

	public async Task<MediaRequest?> DeclineRequest(MediaRequest request)
	{
		return await _apiClient.Request[$"{request.Id}"]["decline"].PostAsync();
	}

	public async Task<MovieDetails?> GetMovieDetails(MediaRequest request)
	{
		if(request.Media?.TmdbId is not { } id)
		{
			return null;
		}

		return await _apiClient.Movie[id].GetAsync();
	}
	public async Task<TvDetails?> GetSeriesDetails(MediaRequest request)
	{
		if (request.Media?.TmdbId is not { } id)
		{
			return null;
		}

		return await _apiClient.Tv[id].GetAsync();
	}
}
