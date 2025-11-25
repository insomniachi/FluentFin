using Jellyfin.Sdk.Generated.Models;

namespace FluentFin.Core.Contracts.Services;

public interface IJumpListService
{
	Task Initialize(IEnumerable<BaseItemDto> libaries);
}
