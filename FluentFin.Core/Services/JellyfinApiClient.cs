using FluentFin.Core.Contracts.Services;
using Flurl;
using Flurl.Http;
using Flurl.Http.Configuration;
using Jellyfin.Client;
using Jellyfin.Client.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FluentFin.Core.Services;

public class JellyfinApiClient(ILogger<JellyfinApiClient> logger) : IJellyfinClient
{
	private const string _authHeaderName = @"X-Emby-Token";
	private string _token = null!;
	private UserDto _user = null!;
	private readonly DefaultJsonSerializer _serializer = new (new JsonSerializerOptions
	{
		Converters =
		{
			new NullableGuidConveter(),
		}
	});

	public Guid UserId { get; set; }
	public string BaseUrl { get; set; } = "";

	public void Initialize(string baseUrl, AuthenticationResult authResult)
	{
		_token = authResult.AccessToken!;
		_user = authResult.User!;

		UserId = authResult.User!.Id!.Value;
		BaseUrl = baseUrl;
	}


	public async Task<BaseItemDtoQueryResult?> GetContinueWatching()
	{
		try
		{
			var result = await BaseUrl.AppendPathSegment($"/Users/{UserId:N}/Items/Resume")
				.SetQueryParams(new
				{
					Limit = 12,
					Recursive = true,
					Fields = "PrimaryImageAspectRatio",
					ImageTypeLimit = 1,
					EnableImageTypes = "Primary,Backdrop,Thumb",
					EnableTotalRecordCount = false,
					MediaTypes = "Video"
				})
				.WithHeader(_authHeaderName, _token)
				.WithSettings(x => x.JsonSerializer = _serializer)
				.GetJsonAsync<BaseItemDtoQueryResult>();

			return result;

		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
			return null;
		}
	}

	public async Task<BaseItemDtoQueryResult?> GetNextUp()
	{
		try
		{
			var result = await BaseUrl.AppendPathSegment($"/Shows/NextUp")
				.SetQueryParams(new
				{
					Limit = 24,
					Fields = "PrimaryImageAspectRatio,DateCreated,Path,MediaSourceCount",
					UserId = UserId.ToString("N"),
					ImageTypeLimit = 1,
					EnableImageTypes = "Primary,Backdrop,Banner,Thumb",
					EnableTotalRecordCount = false,
					Recursive = true,
					DisableFirstEpisode=false,
					NextUpDateCutoff= DateTime.Now.AddYears(-1),
					EnableResumable=false,
					EnableRewatching=false,
				})
				.WithHeader(_authHeaderName, _token)
				.WithSettings(x => x.JsonSerializer = _serializer)
				.GetJsonAsync<BaseItemDtoQueryResult>();

			return result;

		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
			return null;
		}
	}

	public async Task<BaseItemDtoQueryResult?> GetItems(BaseItemDto parent)
	{
		try
		{
			var result = await BaseUrl.AppendPathSegment($"/Users/{UserId}/Items")
				.SetQueryParams(new
				{
					SortBy = "SortName",
					SortOrder = "Ascending",
					Recurisve = "true",
					Fields = "PrimaryImageAspectRatio,DateCreated",
					ImageTypeLimit = 1,
					EnableImageTypes = "Primary,Backdrop,Banner,Thumb",
					ParentId = parent.Id,
					IncludeItemTypes = parent.CollectionType switch
					{
						BaseItemDto_CollectionType.Movies => "Movies",
						BaseItemDto_CollectionType.Tvshows => "Series",
						_ => ""
					}
				})
				.WithHeader(_authHeaderName, _token)
				.WithSettings(x => x.JsonSerializer = _serializer)
				.GetJsonAsync<BaseItemDtoQueryResult>();

			return result;

		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
			return null;
		}
	}

	public async IAsyncEnumerable<NamedDtoQueryResult> GetRecentItemsFromUserLibraries()
	{
		BaseItemDtoQueryResult? views = null;
		try
		{
			views = await BaseUrl.AppendPathSegment($"/UserViews")
				.SetQueryParams(new
				{
					UserId = UserId.ToString("N"),
				})
				.WithHeader(_authHeaderName, _token)
				.WithSettings(x => x.JsonSerializer = _serializer)
				.GetJsonAsync<BaseItemDtoQueryResult>();
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
		}
		
		if(views is null or { Items : null })
		{
			yield break;
		}


		foreach (var library in views.Items)
		{
			if (library is null)
			{
				continue;
			}

			List<BaseItemDto> info = [];
			try
			{
				info = await BaseUrl.AppendPathSegment($"/Users/{UserId:N}/Items/Latest")
									.SetQueryParams(new 
									{
										Limit = 16,
										Fields = "PrimaryImageAspectRatio,Path",
										ImageTypeLimit = 1,
										EnableImageTypes = "Primary,Backdrop,Thumb",
										ParentId = library.Id!.Value.ToString("N")
									})
									.WithHeader(_authHeaderName, _token)
									.WithSettings(x => x.JsonSerializer = _serializer)
									.GetJsonAsync<List<BaseItemDto>>();
			}
			catch (Exception ex)
			{
				logger.LogError(ex, @"Unhandled exception");
			}

			if (info is not null and { Count : > 0 })
			{
				yield return new (library.Name ?? "", info);
			}
		}
	}

	public async IAsyncEnumerable<BaseItemDto> GetUserLibraries()
	{
		BaseItemDtoQueryResult? views = null;
		try
		{
			views = await BaseUrl.AppendPathSegment($"/UserViews")
				.SetQueryParams(new
				{
					UserId = UserId.ToString("N"),
				})
				.WithHeader(_authHeaderName, _token)
				.WithSettings(x => x.JsonSerializer = _serializer)
				.GetJsonAsync<BaseItemDtoQueryResult>();
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
		}

		if (views is null or { Items: null })
		{
			yield break;
		}

		foreach (var item in views.Items)
		{
			yield return item;
		}
	}
}
