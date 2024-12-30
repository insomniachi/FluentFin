using DeviceId;
using FluentFin.Core.Contracts.Services;
using Flurl;
using Jellyfin.Sdk.Generated.Models;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Reflection;

namespace FluentFin.Core.Services;

public class JellyfinClient(ILogger<JellyfinClient> logger) : IJellyfinClient
{
	private Jellyfin.Sdk.JellyfinApiClient _jellyfinApiClient = null!;
	private string _token = "";
	private Jellyfin.Sdk.JellyfinSdkSettings _settings = null!;
	private string _deviceId = "";
	private BaseItemDto? _currentItem = null;

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

	public async Task<BaseItemDtoQueryResult?> GetNextUp(BaseItemDto dto)
	{
		try
		{
			return await _jellyfinApiClient.Shows.NextUp.GetAsync(x =>
			{
				var query = x.QueryParameters;
				if (dto.Type == BaseItemDto_Type.Series)
				{
					query.SeriesId = dto.Id; 
				}
				query.Fields = [ItemFields.MediaSourceCount];
				query.UserId = UserId;
			});
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
			return null;
		}
	}

	public async Task<BaseItemDtoQueryResult?> GetItems(BaseItemDto parent, bool recursive = false)
	{
		try
		{
			return await _jellyfinApiClient.Items.GetAsync(x =>
			{
				var query = x.QueryParameters;
				query.SortBy = [ItemSortBy.SortName];
				query.SortOrder = [SortOrder.Ascending];
				query.Recursive = recursive;
				query.Fields = [ItemFields.PrimaryImageAspectRatio, ItemFields.DateCreated, ItemFields.Overview, ItemFields.Tags, ItemFields.Genres];
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

	public async Task<BaseItemDto?> GetItem(Guid id)
	{
		try
		{
			return await _jellyfinApiClient.Items[id].GetAsync(x => x.QueryParameters.UserId = UserId);
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

	public async Task<BaseItemDtoQueryResult?> GetSimilarItems(BaseItemDto dto)
	{
		if(dto.Id is not { } id)
		{
			return null;
		}

		try
		{
			return await _jellyfinApiClient.Items[id].Similar.GetAsync(x =>
			{
				x.QueryParameters.UserId = UserId;
				x.QueryParameters.Limit = 12;
			});
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
			return null;
		}
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

	public async Task<QueryFiltersLegacy?> GetFilters(BaseItemDto library)
	{
		if(library.Type != BaseItemDto_Type.CollectionFolder)
		{
			return null;
		}

		if(library.Id is not { } id )
		{
			return null;
		}

		try
		{
			 return await _jellyfinApiClient.Items.Filters.GetAsync(x =>
			{
				var query = x.QueryParameters;
				query.UserId = UserId;
				query.ParentId = id;
				query.IncludeItemTypes = library.CollectionType switch
				{
					BaseItemDto_CollectionType.Movies => [BaseItemKind.Movie],
					BaseItemDto_CollectionType.Tvshows => [BaseItemKind.Series],
					_ => []
				};
			});
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
			return null;
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
			var endPointInfo = await EndpointInfo();

			var bitRate = endPointInfo?.IsInNetwork == true ? int.MaxValue : await BitrateTest(); 

			var playbackInfo = await _jellyfinApiClient.Items[id].PlaybackInfo.PostAsync(new PlaybackInfoDto
			{
				UserId = UserId,
				AutoOpenLiveStream = true,
				DeviceProfile = DeviceProfiles.Flyleaf,
				MaxStreamingBitrate = bitRate,
				StartTimeTicks = dto.UserData?.PlaybackPositionTicks
			});

			if(playbackInfo is null or { PlaySessionId : null } or { MediaSources : null })
			{
				return null;
			}

			var sessionId = playbackInfo.PlaySessionId;
			var mediaSource = playbackInfo.MediaSources.FirstOrDefault(x => x.Id == id.ToString("N"));

			if(dto.MediaType == BaseItemDto_MediaType.Video)
			{
				if(!string.IsNullOrEmpty(mediaSource?.TranscodingUrl) && mediaSource?.SupportsTranscoding == true)
				{
					return BaseUrl.AppendPathSegment(mediaSource.TranscodingUrl).ToUri();
				}
				else if(mediaSource?.SupportsDirectPlay == true)
				{
					var info = _jellyfinApiClient.Videos[id].Stream.ToGetRequestInformation(x =>
					{
						var query = x.QueryParameters;
						query.Container = mediaSource.Container;
						query.PlaySessionId = sessionId;
						query.Static = true;
						query.Tag = mediaSource.ETag;
						query.StartTimeTicks = dto.UserData?.PlaybackPositionTicks;
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

		object? requestTag = null;
		var hasRequestTag = dto.ImageTags?.AdditionalData.TryGetValue($"{type}", out requestTag);
		var backdropTag = dto.BackdropImageTags?.FirstOrDefault();
		var parentBackdropTag = dto.ParentBackdropImageTags?.FirstOrDefault();

		var tag = "";
		if (hasRequestTag == true)
		{
			tag = $"{requestTag}";
		}
		else if(!string.IsNullOrEmpty(backdropTag))
		{
			tag = $"{backdropTag}";
		}
		else if(!string.IsNullOrEmpty(parentBackdropTag))
		{
			tag = $"{parentBackdropTag}";
		}

		if(type == ImageType.Backdrop && dto.Type is BaseItemDto_Type.Season && dto.ParentId is { } pid)
		{
			id = pid;
		}

		if (type == ImageType.Thumb && dto.Type == BaseItemDto_Type.Episode)
		{
			type = ImageType.Primary;
		}

		else if (type == ImageType.Primary && dto.Type == BaseItemDto_Type.Episode)
		{
			if (!string.IsNullOrEmpty(dto.SeriesPrimaryImageTag) && dto.SeriesId is { } seriesId)
			{
				id = seriesId;
				tag = $"{dto.SeriesPrimaryImageTag}";
			}
		}



		var uri = BaseUrl.AppendPathSegment($"/Items/{id}/Images/{type}");

		if (height is { } h)
		{
			uri.SetQueryParam("fillHeight", h);
		}

		if(!string.IsNullOrEmpty(tag))
		{
			uri.SetQueryParam("tag", tag);
		}


		return uri.ToUri();
	}

	public Uri GetImage(BaseItemDto item, ImageInfo info)
	{
		return BaseUrl.AppendPathSegment($"/Items/{item.Id}/Images/{info.ImageType}").SetQueryParam("tag", info.ImageTag).ToUri();
	}

	public async Task Playing(BaseItemDto dto)
	{
		try
		{
			await _jellyfinApiClient.Sessions.Playing.PostAsync(new PlaybackStartInfo
			{
				Item = dto,
			});

			_currentItem = dto;
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
		}
	}

	public async Task Progress(BaseItemDto dto, TimeSpan position)
	{
		try
		{
			await _jellyfinApiClient.Sessions.Playing.Progress.PostAsync(new PlaybackProgressInfo
			{
				Item = dto,
				PositionTicks = position.Ticks,
			});
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
	
		}
	}

	public async Task Pause(BaseItemDto dto, TimeSpan? position = null)
	{
		try
		{
			await _jellyfinApiClient.Sessions.Playing.Progress.PostAsync(new PlaybackProgressInfo
			{
				Item = dto,
				IsPaused = true,
				PositionTicks = position?.Ticks
			});
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");

		}
	}

	public async Task Stop(BaseItemDto dto)
	{
		try
		{
			await _jellyfinApiClient.Sessions.Playing.Stopped.PostAsync(new PlaybackStopInfo
			{
				Item = dto
			});

			_currentItem = null;
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
		}
	}

	public async Task Stop()
	{
		if(_currentItem is null)
		{
			return;
		}

		await Stop(_currentItem);
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
		try
		{
			List<int> sizes = [500_000, 1_000_000, 3_000_000];
			List<int> bitRates = new(sizes.Count);

			foreach (var size in sizes)
			{
				var start = TimeProvider.System.GetTimestamp();
				var result = await _jellyfinApiClient.Playback.BitrateTest.GetAsync(x => x.QueryParameters.Size = size);
				var stream = new MemoryStream();
				if (result is null)
				{
					continue;
				}

				await result.CopyToAsync(stream);
				var elapsed = TimeProvider.System.GetElapsedTime(start);

				if (elapsed.TotalSeconds > 10)
				{
					break;
				}

				var length = stream.Length;

				bitRates.Add((int)((length * 8) / elapsed.TotalSeconds));
			}

			return (int)bitRates.Average();
		}
		catch (Exception)
		{

			throw;
		}
	}

	public async Task<List<RemoteSearchResult>> IdentifySeries(BaseItemDto dto, SeriesInfo info)
	{
		try
		{
			return await _jellyfinApiClient.Items.RemoteSearch.Series.PostAsync(new SeriesInfoRemoteSearchQuery
			{
				ItemId = dto.Id,
				SearchInfo = info
			}) ?? [];
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
			return [];
		}
	}

	public async Task<List<RemoteSearchResult>> IdentifyMovie(BaseItemDto dto, MovieInfo info)
	{
		try
		{
			return await _jellyfinApiClient.Items.RemoteSearch.Movie.PostAsync(new MovieInfoRemoteSearchQuery
			{
				ItemId = dto.Id,
				SearchInfo = info
			}) ?? [];
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
			return [];
		}
	}

	public async Task ApplyRemoteResult(BaseItemDto dto, RemoteSearchResult remoteResult)
	{
		if(dto.Id is not { } id)
		{
			return;
		}

		try
		{
			await _jellyfinApiClient.Items.RemoteSearch.Apply[id].PostAsync(remoteResult);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
		}

	}

	public async Task UpdateMetadata(BaseItemDto dto)
	{
		if(dto.Id is not { } id)
		{
			return;
		}

		try
		{
			await _jellyfinApiClient.Items[id].PostAsync(dto);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
		}
	}

	public Uri? GetStreamUrl(BaseItemDto dto)
	{
		if(dto.Id is not { } id)
		{
			return null;
		}

		var info = _jellyfinApiClient.Items[id].Download.ToGetRequestInformation();
		return AddApiKey(info.URI);
	}

	public async Task<List<ExternalIdInfo>> GetExternalIdInfo(BaseItemDto dto)
	{
		if(dto.Id is not { } id)
		{
			return [];
		}

		try
		{
			return await _jellyfinApiClient.Items[id].ExternalIdInfos.GetAsync() ?? [];
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
			return [];
		}
	}

	public async Task<MetadataEditorInfo?> GetMetadataEditorInfo(BaseItemDto dto)
	{
		if (dto.Id is not { } id)
		{
			return null;
		}

		try
		{
			return await _jellyfinApiClient.Items[id].MetadataEditor.GetAsync();
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
			return null;
		}
	}

	public async Task<List<ImageInfo>> GetImages(BaseItemDto dto)
	{
		if (dto.Id is not { } id)
		{
			return [];
		}

		try
		{
			return await _jellyfinApiClient.Items[id].Images.GetAsync() ?? [];
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
			return [];
		}
	}

	public async Task<RemoteImageResult?> SearchImages(BaseItemDto dto, ImageType type, string? providerName = null, bool includeAllLanguages = false)
	{
		if (dto.Id is not { } id)
		{
			return null;
		}

		try
		{
			return await _jellyfinApiClient.Items[id].RemoteImages.GetAsync(x =>
			{
				var query = x.QueryParameters;
				query.StartIndex = 0;
				query.Limit = 30;
				query.IncludeAllLanguages = includeAllLanguages;
				query.ProviderName = providerName;
				query.Type = Enum.Parse<Jellyfin.Sdk.Generated.Items.Item.RemoteImages.ImageType>(type.ToString());
			});
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
			return null;
		}
	}

	public async Task UpdateImage(BaseItemDto dto, RemoteImageInfo info)
	{
		if (dto.Id is not { } id || info.Type is null)
		{
			return;
		}

		try
		{
			await _jellyfinApiClient.Items[id].RemoteImages.Download.PostAsync(x =>
			{
				var query = x.QueryParameters;
				query.ImageUrl = info.Url;
				query.Type = Enum.Parse<Jellyfin.Sdk.Generated.Items.Item.RemoteImages.Download.ImageType>(info.Type.Value.ToString());
			});
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
			return;
		}
	}

	public async Task<List<RemoteSubtitleInfo>> SearchSubtitles(BaseItemDto dto, CultureInfo culture)
	{
		if (dto.Id is not { } id)
		{
			return [];
		}

		try
		{
			return await _jellyfinApiClient.Items[id].RemoteSearch.Subtitles[culture.ThreeLetterISOLanguageName].GetAsync() ?? [];
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
			return [];
		}
	}

	public async Task DownloadSubtitle(BaseItemDto dto, RemoteSubtitleInfo info)
	{
		if (dto.Id is not { } id)
		{
			return;
		}

		try
		{
			await _jellyfinApiClient.Items[id].RemoteSearch.Subtitles[info.Id].PostAsync();
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
			return;
		}
	}

	public async Task DeleteSubtitle(BaseItemDto dto, MediaStream stream)
	{
		if (dto.Id is not { } id)
		{
			return;
		}

		if(stream.Index is null)
		{
			return;
		}

		try
		{
			await _jellyfinApiClient.Videos[id].Subtitles[stream.Index.Value].DeleteAsync();
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
			return;
		}
	}

	public async Task<List<ImageProviderInfo>> GetImageProviders(BaseItemDto dto)
	{
		if (dto.Id is not { } id)
		{
			return [];
		}

		try
		{
			return await _jellyfinApiClient.Items[id].RemoteImages.Providers.GetAsync() ?? [];
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
			return [];
		}
	}

	public async Task RefreshMetadata(BaseItemDto dto, RefreshMetadataInfo info)
	{
		if (dto.Id is not { } id)
		{
			return;
		}

		try
		{
			await _jellyfinApiClient.Items[id].Refresh.PostAsync(x =>
			{
				var query = x.QueryParameters;
				query.ReplaceAllMetadata = info.ReplaceAllMetadata;
				query.ReplaceAllImages = info.ReplaceAllImages;
				query.MetadataRefreshMode = info.MetadataRefreshMode;
				query.ImageRefreshMode = info.ImageRefreshMode;
			});
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
			return;
		}

	}

	public async Task<MediaSegmentDtoQueryResult?> GetMediaSegments(BaseItemDto dto, MediaSegmentType[]? types = null)
	{
		if (dto.Id is not { } id)
		{
			return null;
		}

		try
		{
			return await _jellyfinApiClient.MediaSegments[id].GetAsync(x => x.QueryParameters.IncludeSegmentTypes = types);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
			return null;
		}
	}

	private async Task<EndPointInfo?> EndpointInfo()
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

	private Uri AddApiKey(Uri uri)
	{
		return uri.AppendQueryParam("api_key", _token).ToUri();
	}
}


