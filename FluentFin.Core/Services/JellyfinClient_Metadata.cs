using FluentFin.Core.Contracts.Services;
using Jellyfin.Sdk.Generated.Models;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace FluentFin.Core.Services;

public partial class JellyfinClient
{
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
		if (dto.Id is not { } id)
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
		if (dto.Id is not { } id)
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

	public async Task<List<ExternalIdInfo>> GetExternalIdInfo(BaseItemDto dto)
	{
		if (dto.Id is not { } id)
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

		if (stream.Index is null)
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

	public async Task<QueryFiltersLegacy?> GetFilters(BaseItemDto library)
	{
		if (library.Type != BaseItemDto_Type.CollectionFolder)
		{
			return null;
		}

		if (library.Id is not { } id)
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
}
