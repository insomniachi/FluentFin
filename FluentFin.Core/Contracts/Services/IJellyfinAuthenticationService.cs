namespace FluentFin.Core.Contracts.Services;

public interface IJellyfinAuthenticationService
{
	Task<bool> Authenticate(string url, string username, string password);
}
