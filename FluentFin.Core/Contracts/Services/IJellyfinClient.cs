using System.Collections.ObjectModel;
using System.Globalization;
using FluentFin.Core.Services;
using FluentFin.Core.WebSockets;
using Jellyfin.Sdk.Generated.Items.Item.Refresh;
using Jellyfin.Sdk.Generated.Library.VirtualFolders;
using Jellyfin.Sdk.Generated.Models;

namespace FluentFin.Core.Contracts.Services
{
	public interface IJellyfinClient
	{
		string BaseUrl { get; }

		Guid UserId { get; }

		Task Initialize(string baseUrl, AuthenticationResult authResult);

		Task<EndPointInfo?> EndpointInfo();

		Task<BaseItemDtoQueryResult?> GetContinueWatching();

		Task<BaseItemDtoQueryResult?> GetNextUp();

		Task<BaseItemDtoQueryResult?> GetNextUp(BaseItemDto dto);

		Task<BaseItemDtoQueryResult?> GetItems(BaseItemDto parent, bool recursive = false);

		Task ResetProgress(Guid id);

		Task<BaseItemDtoQueryResult?> GetMediaFolders();

		Task<List<FileSystemEntryInfo>> GetDirectoryContents(string path);

		Task<List<VirtualFolderInfo>> GetVirtualFolders();

		Task DeleteItem(BaseItemDto dto);

		Task CreateLibrary(string name, CollectionTypeOptions collectionType, LibraryOptions options);

		Task DeleteLibrary(string name);

		Task RenameLibrary(string name, string newName);

		Task<BaseItemDtoQueryResult?> GetSimilarItems(BaseItemDto dto);

		Task<BaseItemDtoQueryResult?> Search(string searchTerm);

		Task<BaseItemDto?> GetItem(Guid id);

		IAsyncEnumerable<RecentItemDtoQueryResult> GetRecentItemsFromUserLibraries();

		IAsyncEnumerable<BaseItemDto> GetUserLibraries();

		Task<QueryFiltersLegacy?> GetFilters(BaseItemDto library);

		Task SetIsFavorite(BaseItemDto dto, bool isFavorite);

		Task SetPlayed(BaseItemDto dto, bool played);

		Task<bool> Authenticate(string code);

		Task<MediaResponse?> GetMediaUrl(BaseItemDto dto);

		Uri GetImage(BaseItemDto item, ImageInfo info);

		Task<List<SessionInfoDto>> GetControllableSessions();

		Uri GetTrickplayImage(BaseItemDto dto, int index, int resolution);

		Task Playing(BaseItemDto dto);

		Task Progress(PlaybackProgressInfo info);

		Task Stop(PlaybackStopInfo dto);

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

		Task<List<ParentalRating>> GetParentalRatings();

		Task<List<ImageInfo>> GetImages(BaseItemDto dto);

		Task DeleteImage(BaseItemDto dto, ImageInfo info);

		Task<List<ImageProviderInfo>> GetImageProviders(BaseItemDto dto);

		Task<RemoteImageResult?> SearchImages(BaseItemDto dto, ImageType type, string? providerName = null, bool includeAllLanguages = false);

		Task UpdateImage(BaseItemDto dto, RemoteImageInfo info);

		Task<List<RemoteSubtitleInfo>> SearchSubtitles(BaseItemDto dto, CultureInfo culture);

		Task DownloadSubtitle(BaseItemDto dto, RemoteSubtitleInfo info);

		Task DeleteSubtitle(BaseItemDto dto, MediaStream stream);

		Task RefreshMetadata(Guid id, RefreshMetadataInfo info);

		Task<List<CountryInfo>> GetCountries();

		Task<List<CultureDto>> GetCultures();

		Task<MediaSegmentDtoQueryResult?> GetMediaSegments(BaseItemDto dto, MediaSegmentType[]? types = null);

		Task CreateMediaSegment(MediaSegmentDto segmentDto);

		Task DeleteMediaSegment(Guid segmentId);

		Task<SystemInfo?> GetSystemInfo();

		Task<List<SessionInfoDto>> GetActiveSessions();

		Task<ActivityLogEntryQueryResult?> GetActivities(DateTimeOffset minDate, bool hasUserId);

		Task<ActivityLogEntryQueryResult?> GetAllActivities();

		Task<List<UserDto>> GetUsers();

		Task<UserDto?> GetUser(Guid id);

		Task<UserDto?> CreateUser(string username, string password);

		Task DeleteUser(UserDto user);

		Task UpdatePolicy(UserDto user, UserPolicy policy);

		Task ChangePassword(UserDto user, string currentPassword, string newPassword);

		Task ResetPassword(UserDto user);

		Task<List<TaskInfo>> GetScheduledTasks(bool? isEnabled);

		Task RunTask(string? taskId);

		Task Restart();

		Task Shutdown();

		Task<ServerConfiguration?> GetConfiguration();

		Task<XbmcMetadata> GetXbmcMetadata();

		Task SaveXbmcMetadata(XbmcMetadata metadata);

		Task<Metadata> GetMetadata();

		Task SaveMetadata(Metadata metadata);

		Task SaveConfiguration(ServerConfiguration configuration);

		Task<TranscodingSettings?> GetTranscodeOptions();

		Task SaveTranscodeOptions(TranscodingSettings options);

		Task<LibraryOptionsResultDto?> GetAvailableInfo(Jellyfin.Sdk.Generated.Libraries.AvailableOptions.CollectionType libraryContentType, bool isNewLibrary);

		Task SaveLibraryOptions(Guid folderId, LibraryOptions options);

		Task<List<TaskInfo>> GetScheduledTasks();

		Task RunScheduledTask(string id);

		Task PlayOnSession(string sessionId, params IEnumerable<Guid?> itemIds);

		Task TogglePlayPause(string sessionId);

		Task Stop(string sessionId);

		Task SendWebsocketMessage<T>(T message) where T: WebSocketMessage;

		// SyncPlay
		Task<List<GroupInfoDto>> GetSyncPlayGroups();

		Task CreateSyncPlayGroup();

		Task JoinSyncPlayGroup(Guid groupId);

		Task LeaveSyncPlayGroup();

		Task SignalReadyForSyncPlay(ReadyRequestDto request);

		Task SignalPauseForSyncPlay();

		Task SignalUnpauseForSyncPlay();

		Task SignalSeekForSyncPlay(TimeSpan position);

		Task SignalNewPlaylist(PlayRequestDto request);

		Task<DateTimeOffset> SyncTime();

		Task SendMessage(string sessionId, MessageCommand command);
	}

	public record RecentItemDtoQueryResult(BaseItemDto Library, ObservableCollection<BaseItemDto> Items);

	public record RefreshMetadataInfo(MetadataRefreshMode ImageRefreshMode, MetadataRefreshMode MetadataRefreshMode, bool ReplaceAllImages, bool RegenerateTrickplay, bool ReplaceAllMetadata);

	public record MediaResponse(Uri Uri, PlaybackProgressInfo_PlayMethod PlayMethod, string PlaybackSessionId, string MediaSourceId, MediaSourceInfo MediaSourceInfo);

	public class XbmcMetadata
	{
		public string? ReleaseDateFormat { get; set; }
		public bool SaveImagePathsInNfo { get; set; }
		public bool EnablePathSubstitution { get; set; }
		public bool EnableExtraThumbsDuplication { get; set; }
		public Guid? UserId { get; set; }
	}

	public class Metadata
	{
		public bool UseFileCreationTimeForDateAdded { get; set; }
	}

	public static class JellyfinClientExtensions
	{
		public static async Task SendWebSocketMessageWithoutData<T>(this IJellyfinClient client)
			where T: WebSocketMessage, new()
		{
			await client.SendWebsocketMessage(new T());
		}
	}
}
