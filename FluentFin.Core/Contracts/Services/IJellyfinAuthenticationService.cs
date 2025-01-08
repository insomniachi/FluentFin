using FluentFin.Core.Settings;

namespace FluentFin.Core.Contracts.Services;

public interface IJellyfinAuthenticationService
{
	Task<bool> Authenticate(SavedServer server, string username, string password, bool remember);
	Task<bool> Authenticate(SavedServer server, SavedUser user);
}
