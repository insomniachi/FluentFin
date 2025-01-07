using FluentFin.Contracts.Services;
using FluentFin.Core;
using FluentFin.Core.Contracts.Services;
using FluentFin.Core.Settings;
using FluentFin.Core.ViewModels;
using FluentFin.Helpers;
using FluentFin.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using System.Security.Cryptography;
using System.Text;

namespace FluentFin.Activation;

public class DefaultActivationHandler : ActivationHandler<LaunchActivatedEventArgs>
{
    private readonly INavigationService _navigationService;
    private readonly ISettings _settings;
	private readonly IJellyfinAuthenticationService _jellyfinAuthenticationService;
    private readonly ILocalSettingsService _settingsService;
    private readonly INavigationService _coreNavigationService;

	public DefaultActivationHandler([FromKeyedServices(NavigationRegions.InitialSetup)]INavigationService navigationService,
                                    INavigationService coreNavigationService,
                                    ISettings settings,
                                    IJellyfinAuthenticationService jellyfinAuthenticationService,
                                    ILocalSettingsService settingsService)
    {
        _navigationService = navigationService;
        _coreNavigationService = coreNavigationService;
        _settings = settings;
		_jellyfinAuthenticationService = jellyfinAuthenticationService;
        _settingsService = settingsService;
	}

    protected override bool CanHandleInternal(LaunchActivatedEventArgs args)
    {
        // None of the ActivationHandlers has handled the activation.
        return _navigationService.Frame?.Content == null;
    }

    protected async override Task HandleInternalAsync(LaunchActivatedEventArgs args)
    {
        var servers = _settings.Servers;

        if(_settings.Servers.Count == 1)
        {
            var server = _settings.Servers[0];
            var users = _settings.Users.Where(x => x.ServerId == server.Id).ToList();

            if(users.Count == 1)
            {
                var user = users[0];
                var password = Encoding.UTF8.GetString(ProtectedData.Unprotect(user.Password, _settingsService.GetEntropyBytes(), DataProtectionScope.CurrentUser));
                var result = await _jellyfinAuthenticationService.Authenticate(server.GetServerUrl(), user.Username, password);

                if(result)
                {
                    _navigationService.NavigateTo(typeof(ShellViewModel).FullName!);
                    _coreNavigationService.NavigateTo(typeof(HomeViewModel).FullName!);
				}
                else
                {
					_navigationService.NavigateTo(typeof(SelectServerViewModel).FullName!);
				}

            }
        }
        else
        {
			_navigationService.NavigateTo(typeof(SelectServerViewModel).FullName!);
		}
    }
}
