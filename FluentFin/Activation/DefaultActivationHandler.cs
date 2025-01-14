using FluentFin.Contracts.Services;
using FluentFin.Core;
using FluentFin.Core.Contracts.Services;
using FluentFin.Core.Settings;
using FluentFin.Core.ViewModels;
using FluentFin.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;

namespace FluentFin.Activation;

public class DefaultActivationHandler : ActivationHandler<LaunchActivatedEventArgs>
{
	private readonly INavigationService _navigationService;
	private readonly ISettings _settings;
	private readonly IJellyfinAuthenticationService _jellyfinAuthenticationService;
	private readonly ILocalSettingsService _settingsService;
	private readonly INavigationService _coreNavigationService;

	public DefaultActivationHandler([FromKeyedServices(NavigationRegions.InitialSetup)] INavigationService navigationService,
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

		_settings.ListenToChanges();
	}

	protected override bool CanHandleInternal(LaunchActivatedEventArgs args)
	{
		// None of the ActivationHandlers has handled the activation.
		return _navigationService.Frame?.Content == null;
	}

	protected async override Task HandleInternalAsync(LaunchActivatedEventArgs args)
	{
		if (_settings.Servers.Count == 1 && _settings.Servers[0].Users.Count == 1)
		{
			var result = await _jellyfinAuthenticationService.Authenticate(_settings.Servers[0], _settings.Servers[0].Users[0]);

			if (result)
			{
				_navigationService.NavigateTo(typeof(ShellViewModel).FullName!);
			}
			else
			{
				_navigationService.NavigateTo(typeof(SelectServerViewModel).FullName!);
			}
		}
		else
		{
			_navigationService.NavigateTo(typeof(SelectServerViewModel).FullName!);
		}
	}
}
