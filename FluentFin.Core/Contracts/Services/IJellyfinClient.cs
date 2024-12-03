using Jellyfin.Client.Models;

namespace FluentFin.Core.Contracts.Services
{
	public interface IJellyfinClient
	{
		public void Initialize(string baseUrl, AuthenticationResult authResult);

		public Task<BaseItemDtoQueryResult?> GetContinueWatching();
	}
}
