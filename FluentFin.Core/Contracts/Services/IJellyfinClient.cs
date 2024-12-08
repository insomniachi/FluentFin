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

		Task<BaseItemDtoQueryResult?> GetItems(BaseItemDto parent);
		
		IAsyncEnumerable<NamedDtoQueryResult> GetRecentItemsFromUserLibraries();

		IAsyncEnumerable<BaseItemDto> GetUserLibraries();

		Task<UserItemDataDto?> ToggleMarkAsFavorite(BaseItemDto dto);

		Task<UserItemDataDto?> ToggleMarkAsWatched(BaseItemDto dto);

	}

	public record NamedDtoQueryResult(string Name, List<BaseItemDto> Items);
}
