using FluentFin.Core.Settings;
using Jellyfin.Sdk.Generated.Models;

namespace FluentFin.Core.Contracts.Services;

public interface IJellyfinAuthenticationService
{
	/// <returns><c>null</c> on success, otherwise a message describing why sign-in failed.</returns>
	Task<string?> Authenticate(SavedServer server, string username, string password, bool remember);
	Task<bool> Authenticate(SavedServer server, SavedUser user);
	Task<bool> Authenticate(SavedServer server, QuickConnectResult result);
	Task<QuickConnectResult?> GetQuickConnectCode(SavedServer server);
	Task<QuickConnectResult?> CheckQuickConnectStatus(SavedServer server, QuickConnectResult result);
}
