using DeviceId;
using FluentFin.Core.Contracts.Services;
using Flurl;
using Jellyfin.Sdk.Generated.Models;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace FluentFin.Core.Services;

public class JellyfinClient(ILogger<JellyfinClient> logger) : IJellyfinClient
{
	private Jellyfin.Sdk.JellyfinApiClient _jellyfinApiClient = null!;
	private string _token = "";
	private Jellyfin.Sdk.JellyfinSdkSettings _settings = null!;
	private string _deviceId = "";

	public Guid UserId { get; set; }
	public string BaseUrl { get; set; } = "";

	public void Initialize(string baseUrl, AuthenticationResult authResult)
	{
		ArgumentNullException.ThrowIfNull(authResult.User);
		ArgumentNullException.ThrowIfNull(authResult.User.Id);
		ArgumentNullException.ThrowIfNullOrEmpty(authResult.AccessToken);

		_token = authResult.AccessToken;

		UserId = authResult.User.Id.Value;
		BaseUrl = baseUrl;

		_deviceId = new DeviceIdBuilder().OnWindows(windows => windows.AddWindowsDeviceId()).ToString();
		_settings = new Jellyfin.Sdk.JellyfinSdkSettings();
		_settings.Initialize("FluentFin", Assembly.GetEntryAssembly()!.GetName().Version!.ToString()!, Environment.MachineName, _deviceId);
		_settings.SetAccessToken(_token);
		_settings.SetServerUrl(baseUrl);
		_jellyfinApiClient = new Jellyfin.Sdk.JellyfinApiClient(new Jellyfin.Sdk.JellyfinRequestAdapter(new Jellyfin.Sdk.JellyfinAuthenticationProvider(_settings), _settings));
	}


	public async Task<BaseItemDtoQueryResult?> GetContinueWatching()
	{
		try
		{
			return await _jellyfinApiClient.UserItems.Resume.GetAsync(x =>
			{
				var query = x.QueryParameters;
				query.Limit = 12;
				query.Fields = [ItemFields.PrimaryImageAspectRatio];
				query.ImageTypeLimit = 1;
				query.EnableImageTypes = [ImageType.Primary, ImageType.Backdrop, ImageType.Thumb];
				query.EnableTotalRecordCount = false;
				query.MediaTypes = [MediaType.Video];
			});
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
			return await _jellyfinApiClient.Shows.NextUp.GetAsync(x =>
			{
				var query = x.QueryParameters;
				query.Limit = 24;
				query.Fields = [ItemFields.PrimaryImageAspectRatio, ItemFields.DateCreated, ItemFields.Path, ItemFields.MediaSourceCount];
				query.UserId = UserId;
				query.ImageTypeLimit = 1;
				query.EnableImageTypes = [ImageType.Primary, ImageType.Backdrop, ImageType.Banner, ImageType.Thumb];
			});
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
			return await _jellyfinApiClient.Items.GetAsync(x =>
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

	public async Task<UserItemDataDto?> ToggleMarkAsFavorite(BaseItemDto dto)
	{
		try
		{
			if(dto.Id is null)
			{
				return null;
			}

			return await _jellyfinApiClient.UserItems[dto.Id.Value].UserData.PostAsync(new UpdateUserItemDataDto
			{
				IsFavorite = !(dto.UserData?.IsFavorite ?? false)
			});
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
			return null;
		}
	}

	public async Task<UserItemDataDto?> ToggleMarkAsWatched(BaseItemDto dto)
	{
		try
		{
			if (dto.Id is null)
			{
				return null;
			}

			return await _jellyfinApiClient.UserItems[dto.Id.Value].UserData.PostAsync(new UpdateUserItemDataDto
			{
				Played = !(dto.UserData?.Played ?? false)
			});
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
			return null;
		}
	}

	public async Task<Uri?> GetMediaUrl(BaseItemDto dto)
	{
		if(dto.Id is not { } id)
		{
			return null;
		}

		if(dto.Type is BaseItemDto_Type.Movie or BaseItemDto_Type.Episode)
		{
			var playbackInfo = await _jellyfinApiClient.Items[id].PlaybackInfo.PostAsync(new PlaybackInfoDto
			{
				UserId = UserId,
				AutoOpenLiveStream = true,
			});

			if(playbackInfo is null or { PlaySessionId : null } or { MediaSources : null })
			{
				return null;
			}

			var sessionId = playbackInfo.PlaySessionId;
			var mediaSource = playbackInfo.MediaSources.FirstOrDefault(x => x.Id == id.ToString("N"));

			if(mediaSource is null)
			{
				return null;
			}

			if(dto.MediaType == BaseItemDto_MediaType.Video)
			{
				if(!string.IsNullOrEmpty(mediaSource.TranscodingUrl))
				{
					return BaseUrl.AppendPathSegment(mediaSource.TranscodingUrl).SetQueryParams(new
					{
						api_key = _token,
						SubtitleMethod = "Hls"
					}).ToUri();
				}
				else if(mediaSource.SupportsDirectPlay == true)
				{
					var info = _jellyfinApiClient.Videos[id].Stream.ToGetRequestInformation(x =>
					{
						var query = x.QueryParameters;
						query.Container = mediaSource.Container;
						query.PlaySessionId = sessionId;
						query.Static = true;
						query.Tag = mediaSource.ETag;
					});

					return AddApiKey(info.URI);
				}
			}
		}

		return null;
	}

	public Uri? GetImage(BaseItemDto dto, ImageType type, double? height = null)
	{
		if(dto.Id is not { } id)
		{
			return null;
		}

		if(dto is { ImageTags: null } or { BackdropImageTags : null or { Count : 0} })
		{
			return BaseUrl.AppendPathSegment($"/Items/{id}/Images/{type}").ToUri();
		}

		var hasrequestTag = dto.ImageTags.AdditionalData.TryGetValue($"{type}", out var requestTag);
		var backdropTag = dto.BackdropImageTags?[0];
		var parentBackdropTag = dto.ParentBackdropImageTags?[0];

		var uri = BaseUrl.AppendPathSegment($"/Items/{id}/Images/{type}");

		if(height is { } h)
		{
			uri.SetQueryParam("fillHeight", h);
		}

		if(hasrequestTag)
		{
			uri.SetQueryParam("tag", requestTag);
		}
		else if(!string.IsNullOrEmpty(backdropTag))
		{
			uri.SetQueryParam("tag", backdropTag);
		}
		else if(!string.IsNullOrEmpty(parentBackdropTag))
		{
			uri.SetQueryParam("tag", parentBackdropTag);
		}

		return uri.ToUri();
	}

	private Uri AddApiKey(Uri uri)
	{
		return uri.AppendQueryParam("api_key", _token).ToUri();
	}
}


