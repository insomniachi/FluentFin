using DeviceId;
using FluentFin.Core.Contracts.Services;
using Jellyfin.Sdk.Generated.Models;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace FluentFin.Core.Services;

public class JellyfinClient(ILogger<JellyfinClient> logger) : IJellyfinClient
{
	private Jellyfin.Sdk.JellyfinApiClient _jellyfinApiClient = null!;
	private string _token = null!;
	private UserDto _user = null!;

	public Guid UserId { get; set; }
	public string BaseUrl { get; set; } = "";

	public void Initialize(string baseUrl, AuthenticationResult authResult)
	{
		_token = authResult.AccessToken!;
		_user = authResult.User!;

		UserId = authResult.User!.Id!.Value;
		BaseUrl = baseUrl;

		var id = new DeviceIdBuilder().OnWindows(windows => windows.AddWindowsDeviceId()).ToString();
		var settings = new Jellyfin.Sdk.JellyfinSdkSettings();
		settings.Initialize("FluentFin", Assembly.GetEntryAssembly()!.GetName().Version!.ToString()!, Environment.MachineName, id);
		settings.SetAccessToken(_token);
		settings.SetServerUrl(baseUrl);
		_jellyfinApiClient = new Jellyfin.Sdk.JellyfinApiClient(new Jellyfin.Sdk.JellyfinRequestAdapter(new Jellyfin.Sdk.JellyfinAuthenticationProvider(settings), settings));
	}


	public async Task<BaseItemDtoQueryResult?> GetContinueWatching()
	{
		try
		{
			var result = await _jellyfinApiClient.UserItems.Resume.GetAsync(x =>
			{
				var query = x.QueryParameters;
				query.Limit = 12;
				query.Fields = [ItemFields.PrimaryImageAspectRatio];
				query.ImageTypeLimit = 1;
				query.EnableImageTypes = [ImageType.Primary, ImageType.Backdrop, ImageType.Thumb];
				query.EnableTotalRecordCount = false;
				query.MediaTypes = [MediaType.Video];
			});

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
			var result = await _jellyfinApiClient.Shows.NextUp.GetAsync(x =>
			{
				var query = x.QueryParameters;
				query.Limit = 24;
				query.Fields = [ItemFields.PrimaryImageAspectRatio, ItemFields.DateCreated, ItemFields.Path, ItemFields.MediaSourceCount];
				query.UserId = UserId;
				query.ImageTypeLimit = 1;
				query.EnableImageTypes = [ImageType.Primary, ImageType.Backdrop, ImageType.Banner, ImageType.Thumb];
			});

			//var result = await BaseUrl.AppendPathSegment($"/Shows/NextUp")
			//	.SetQueryParams(new
			//	{
			//		Limit = 24,
			//		Fields = "PrimaryImageAspectRatio,DateCreated,Path,MediaSourceCount",
			//		UserId = UserId.ToString("N"),
			//		ImageTypeLimit = 1,
			//		EnableImageTypes = "Primary,Backdrop,Banner,Thumb",
			//		EnableTotalRecordCount = false,
			//		Recursive = true,
			//		DisableFirstEpisode=false,
			//		NextUpDateCutoff= DateTime.Now.AddYears(-1),
			//		EnableResumable=false,
			//		EnableRewatching=false,
			//	})
			//	.WithHeader(_authHeaderName, _token)
			//	.WithSettings(x => x.JsonSerializer = _serializer)
			//	.GetJsonAsync<BaseItemDtoQueryResult>();

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
			var result = await _jellyfinApiClient.Items.GetAsync(x =>
			{
				var query = x.QueryParameters;
				query.SortBy = [ItemSortBy.SortName];
				query.SortOrder = [SortOrder.Ascending];
				query.Recursive = true;
				query.Fields = [ItemFields.PrimaryImageAspectRatio, ItemFields.DateCreated];
				query.ImageTypeLimit = 1;
				query.EnableImageTypes = [ImageType.Primary, ImageType.Backdrop, ImageType.Banner, ImageType.Thumb];
				query.ParentId = parent.Id;
				query.IncludeItemTypes = parent.CollectionType switch
				{
					BaseItemDto_CollectionType.Movies => [BaseItemKind.Movie],
					BaseItemDto_CollectionType.Tvshows => [BaseItemKind.Series],
					_ => null
				};
			});
			//var result = await BaseUrl.AppendPathSegment($"/Users/{UserId}/Items")
			//	.SetQueryParams(new
			//	{
			//		SortBy = "SortName",
			//		SortOrder = "Ascending",
			//		Recurisve = "true",
			//		Fields = "PrimaryImageAspectRatio,DateCreated",
			//		ImageTypeLimit = 1,
			//		EnableImageTypes = "Primary,Backdrop,Banner,Thumb",
			//		ParentId = parent.Id,
			//		IncludeItemTypes = parent.CollectionType switch
			//		{
			//			BaseItemDto_CollectionType.Movies => "Movies",
			//			BaseItemDto_CollectionType.Tvshows => "Series",
			//			_ => ""
			//		}
			//	})
			//	.WithHeader(_authHeaderName, _token)
			//	.WithSettings(x => x.JsonSerializer = _serializer)
			//	.GetJsonAsync<BaseItemDtoQueryResult>();

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
			views = await _jellyfinApiClient.UserViews.GetAsync(x => x.QueryParameters.UserId = UserId);

			//views = await BaseUrl.AppendPathSegment($"/UserViews")
			//	.SetQueryParams(new
			//	{
			//		UserId = UserId.ToString("N"),
			//	})
			//	.WithHeader(_authHeaderName, _token)
			//	.WithSettings(x => x.JsonSerializer = _serializer)
			//	.GetJsonAsync<BaseItemDtoQueryResult>();
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

			List<BaseItemDto>? info = [];
			try
			{
				info = await _jellyfinApiClient.Items.Latest.GetAsync(x =>
				{
					var query = x.QueryParameters;
					query.UserId = UserId;
					query.Limit = 16;
					query.ImageTypeLimit = 1;
					query.EnableImageTypes = [ImageType.Primary, ImageType.Backdrop, ImageType.Thumb];
					query.ParentId = library.Id;
				});
				//info = await BaseUrl.AppendPathSegment($"/Users/{UserId:N}/Items/Latest")
				//					.SetQueryParams(new 
				//					{
				//						Limit = 16,
				//						Fields = "PrimaryImageAspectRatio,Path",
				//						ImageTypeLimit = 1,
				//						EnableImageTypes = "Primary,Backdrop,Thumb",
				//						ParentId = library.Id!.Value.ToString("N")
				//					})
				//					.WithHeader(_authHeaderName, _token)
				//					.WithSettings(x => x.JsonSerializer = _serializer)
				//					.GetJsonAsync<List<BaseItemDto>>();
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
			views = await _jellyfinApiClient.UserViews.GetAsync(x => x.QueryParameters.UserId = UserId);
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
