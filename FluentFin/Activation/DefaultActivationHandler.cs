using FluentFin.Contracts.Services;
using FluentFin.Core;
using FluentFin.Core.Contracts.Services;
using FluentFin.Core.Services;
using FluentFin.Core.Settings;
using FluentFin.Core.ViewModels;
using FluentFin.UI.Core;
using FluentFin.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;

namespace FluentFin.Activation;

public class DefaultActivationHandler : ActivationHandler<LaunchActivatedEventArgs>
{
	private readonly INavigationService _navigationService;
	private readonly INavigationService _mainNavigationService;
	private readonly ISettings _settings;
	private readonly IJellyfinAuthenticationService _jellyfinAuthenticationService;
	private readonly ILocalSettingsService _localSettingsService;
	private readonly IPluginManager _pluginManager;

	public DefaultActivationHandler([FromKeyedServices(NavigationRegions.InitialSetup)] INavigationService navigationService,
									INavigationService mainNavigationService,
									ISettings settings,
									ILocalSettingsService localSettingsService,
									IPluginManager pluginManager,
									IJellyfinAuthenticationService jellyfinAuthenticationService)
	{
		_navigationService = navigationService;
		_mainNavigationService = mainNavigationService;
		_settings = settings;
		_jellyfinAuthenticationService = jellyfinAuthenticationService;
		_localSettingsService = localSettingsService;
		_pluginManager = pluginManager;
		_settings.ListenToChanges();
	}

	protected override bool CanHandleInternal(LaunchActivatedEventArgs args)
	{
		// None of the ActivationHandlers has handled the activation.
		return _navigationService.Frame?.Content == null;
	}

	protected async override Task HandleInternalAsync(LaunchActivatedEventArgs args)
	{
		_pluginManager.LoadOptions(_localSettingsService);

		if (_settings.Servers.Count == 1 && _settings.Servers[0].Users.Count == 1)
		{
			var result = await _jellyfinAuthenticationService.Authenticate(_settings.Servers[0], _settings.Servers[0].Users[0]);

			if (result)
			{
				_navigationService.NavigateTo<ShellViewModel>();

				var cmdArgs = Environment.GetCommandLineArgs();
				if(cmdArgs.Length == 2 && Guid.TryParse(cmdArgs[1], out var libraryId))
				{
					_mainNavigationService.NavigateTo<LibraryViewModel>(libraryId);
				}
			}
			else
			{
				_navigationService.NavigateTo<SelectServerViewModel>();
			}
		}
		else
		{
			_navigationService.NavigateTo<SelectServerViewModel>();
		}
	}
}
