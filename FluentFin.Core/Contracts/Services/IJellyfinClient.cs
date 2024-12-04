using FluentFin.Core.Services;
using Jellyfin.Client.Models;

namespace FluentFin.Core.Contracts.Services
{
	public interface IJellyfinClient
	{
		string BaseUrl { get; }

		Guid UserId { get; }

		void Initialize(string baseUrl, AuthenticationResult authResult);

		Task<BaseItemDtoQueryResult?> GetContinueWatching();

		Task<BaseItemDtoQueryResult?> GetNextUp();
		
		IAsyncEnumerable<NamedDtoQueryResult> GetRecentItemsFromUserLibraries();

		IAsyncEnumerable<BaseItemDto> GetUserLibraries();

	}

	public record NamedDtoQueryResult(string Name, List<BaseItemDto> Items);
}
