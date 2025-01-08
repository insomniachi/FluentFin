using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentFin.Contracts.Services;
using FluentFin.Contracts.ViewModels;
using FluentFin.Core;
using FluentFin.Core.Contracts.Services;
using FluentFin.Core.Settings;
using FluentFin.Helpers;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using System.Reactive.Linq;

namespace FluentFin.ViewModels;

public partial class LoginViewModel : ObservableObject, INavigationAware
{
	private readonly IJellyfinAuthenticationService _jellyfinAuthenticator;
	private readonly ILocalSettingsService _settingsService;

	public LoginViewModel(IJellyfinAuthenticationService jellyfinAuthenticator,
						  INavigationService navigationService,
						  [FromKeyedServices(NavigationRegions.InitialSetup)]INavigationService setupNavigationServer,
						  INavigationViewService navigationViewService,
						  ILocalSettingsService settingsService)
	{
		_jellyfinAuthenticator = jellyfinAuthenticator;
		_settingsService = settingsService;


		this.WhenAnyValue(x => x.Username, x => x.Password)
			.Select(x => !string.IsNullOrEmpty(x.Item1) && !string.IsNullOrEmpty(x.Item2) && Server is not null)
			.Subscribe(validDetails => CanLogin = validDetails);
	}


	[ObservableProperty]
	public partial string Username { get; set; } = "";

	[ObservableProperty]
	public partial string Password { get; set; } = "";

	[ObservableProperty]
	public partial bool KeepMeLoggedIn { get; set; }

	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(LoginCommand))]
	public partial bool CanLogin { get; set; }

	[ObservableProperty]
	public partial SavedServer? Server { get; private set; }


	[RelayCommand(CanExecute = nameof(CanLogin))]
	public async Task Login()
	{
		if(Server is null)
		{
			return;
		}

		var success = await _jellyfinAuthenticator.Authenticate(Server, Username, Password, KeepMeLoggedIn);

		if(!success)
		{
			// message ?
		}
	}

	public Task OnNavigatedFrom() => Task.CompletedTask;

	public Task OnNavigatedTo(object parameter)
	{
		if(parameter is not SavedServer server)
		{
			return Task.CompletedTask;
		}

		Server = server;
		return Task.CompletedTask;
	}

	public string Unprotect(byte[] protectedBytes) => protectedBytes.Unprotect(_settingsService.GetEntropyBytes());
}
