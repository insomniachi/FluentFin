using Jellyfin.Sdk.Generated.Items.Item.Refresh;
using Jellyfin.Sdk.Generated.Models;
using System.Globalization;

namespace FluentFin.Core.Contracts.Services
{
	public interface IJellyfinClient
	{
		string BaseUrl { get; }

		Guid UserId { get; }

		Task Initialize(string baseUrl, AuthenticationResult authResult);

		Task<BaseItemDtoQueryResult?> GetContinueWatching();

		Task<BaseItemDtoQueryResult?> GetNextUp();

		Task<BaseItemDtoQueryResult?> GetNextUp(BaseItemDto dto);

		Task<BaseItemDtoQueryResult?> GetItems(BaseItemDto parent, bool recursive = false);

		Task<BaseItemDtoQueryResult?> GetMediaFolders();

		Task<BaseItemDtoQueryResult?> GetSimilarItems(BaseItemDto dto);

		Task<BaseItemDtoQueryResult?> Search(string searchTerm);

		Task<BaseItemDto?> GetItem(Guid id);
		
		IAsyncEnumerable<NamedDtoQueryResult> GetRecentItemsFromUserLibraries();

		IAsyncEnumerable<BaseItemDto> GetUserLibraries();

		Task<QueryFiltersLegacy?> GetFilters(BaseItemDto library);

		Task<UserItemDataDto?> ToggleMarkAsFavorite(BaseItemDto dto);

		Task<UserItemDataDto?> ToggleMarkAsWatched(BaseItemDto dto);

		Task<Uri?> GetMediaUrl(BaseItemDto dto);

		Uri? GetImage(BaseItemDto dto, ImageType type, double? height = null);

		Uri GetImage(BaseItemDto item, ImageInfo info);

		Task Playing(BaseItemDto dto);

		Task Progress(BaseItemDto dto, TimeSpan position);

		Task Pause(BaseItemDto dto, TimeSpan? position = null);

		Task Stop(BaseItemDto dto);

		Task Stop();

		Task Logout();

		Task<int> BitrateTest();
		
		Task<List<RemoteSearchResult>> IdentifySeries(BaseItemDto dto, SeriesInfo info);
		
		Task<List<RemoteSearchResult>> IdentifyMovie(BaseItemDto dto, MovieInfo info);
		
		Task ApplyRemoteResult(BaseItemDto dto, RemoteSearchResult remoteResult);

		Task UpdateMetadata(BaseItemDto dto);

		Uri? GetStreamUrl(BaseItemDto dto);

		Task<List<ExternalIdInfo>> GetExternalIdInfo(BaseItemDto dto);

		Task<MetadataEditorInfo?> GetMetadataEditorInfo(BaseItemDto dto);

		Task<List<ImageInfo>> GetImages(BaseItemDto dto);

		Task<List<ImageProviderInfo>> GetImageProviders(BaseItemDto dto);

		Task<RemoteImageResult?> SearchImages(BaseItemDto dto, ImageType type, string? providerName = null, bool includeAllLanguages = false);

		Task UpdateImage(BaseItemDto dto, RemoteImageInfo info);

		Task<List<RemoteSubtitleInfo>> SearchSubtitles(BaseItemDto dto, CultureInfo culture);

		Task DownloadSubtitle(BaseItemDto dto, RemoteSubtitleInfo info);

		Task DeleteSubtitle(BaseItemDto dto, MediaStream stream);

		Task RefreshMetadata(BaseItemDto dto, RefreshMetadataInfo info);

		Task<MediaSegmentDtoQueryResult?> GetMediaSegments(BaseItemDto dto, MediaSegmentType[]? types = null);

		Task<SystemInfo?> GetSystemInfo();

		Task<List<SessionInfoDto>> GetActiveSessions();

		Task<ActivityLogEntryQueryResult?> GetActivities(DateTimeOffset minDate, bool hasUserId);

		Task<List<UserDto>> GetUsers();

		Task<UserDto?> GetUser(Guid id);

		Task<UserDto?> CreateUser(string username, string password);

		Task DeleteUser(UserDto user);

		Task UpdatePolicy(UserDto user, UserPolicy policy);

		Task<List<TaskInfo>> GetScheduledTasks(bool? isEnabled);

		Task RunTask(string? taskId);

		Task Restart();

		Task Shutdown();
	}

	public record NamedDtoQueryResult(string Name, List<BaseItemDto> Items);

	public record RefreshMetadataInfo(MetadataRefreshMode ImageRefreshMode, MetadataRefreshMode MetadataRefreshMode, bool ReplaceAllImages, bool RegenerateTrickplay, bool ReplaceAllMetadata);
}
