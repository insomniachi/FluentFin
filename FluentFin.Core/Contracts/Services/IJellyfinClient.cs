using Jellyfin.Sdk.Generated.Models;

namespace FluentFin.Core.Contracts.Services
{
	public interface IJellyfinClient
	{
		string BaseUrl { get; }

		Guid UserId { get; }

		void Initialize(string baseUrl, AuthenticationResult authResult);

		Task<BaseItemDtoQueryResult?> GetContinueWatching();

		Task<BaseItemDtoQueryResult?> GetNextUp();

		Task<BaseItemDtoQueryResult?> GetNextUp(BaseItemDto dto);

		Task<BaseItemDtoQueryResult?> GetItems(BaseItemDto parent, bool recursive = false);

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

		Task Playing(BaseItemDto dto);

		Task Progress(BaseItemDto dto, TimeSpan position);

		Task Pause(BaseItemDto dto, TimeSpan? position = null);

		Task Stop(BaseItemDto dto);

		Task Stop();

		Task Logout();

		Task BitrateTest();
		
		Task<List<RemoteSearchResult>> IdentifySeries(BaseItemDto dto, SeriesInfo info);
		
		Task<List<RemoteSearchResult>> IdentifyMovie(BaseItemDto dto, MovieInfo info);
		
		Task ApplyRemoteResult(BaseItemDto dto, RemoteSearchResult remoteResult);

		Task UpdateMetadata(BaseItemDto dto);

		Uri? GetStreamUrl(BaseItemDto dto);

		Task<List<ExternalIdInfo>> GetExternalIdInfo(BaseItemDto dto);
	}

	public record NamedDtoQueryResult(string Name, List<BaseItemDto> Items);
}
