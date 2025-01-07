using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentFin.Contracts.Services;
using FluentFin.Contracts.ViewModels;
using FluentFin.Core;
using FluentFin.Core.Contracts.Services;
using FluentFin.Core.Settings;
using FluentFin.Core.ViewModels;
using FluentFin.Helpers;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using System.Reactive.Linq;
using System.Security.Cryptography;
using System.Text;

namespace FluentFin.ViewModels;

public partial class LoginViewModel : ObservableObject, INavigationAware
{
	private readonly IJellyfinAuthenticationService _jellyfinAuthenticator;
	private readonly INavigationService _navigationService;
	private readonly INavigationService _setupNavigationService;
	private readonly INavigationViewService _navigationViewService;
	private readonly ILocalSettingsService _settingsService;

	public LoginViewModel(IJellyfinAuthenticationService jellyfinAuthenticator,
						  INavigationService navigationService,
						  [FromKeyedServices(NavigationRegions.InitialSetup)]INavigationService setupNavigationServer,
						  INavigationViewService navigationViewService,
						  ILocalSettingsService settingsService)
	{
		_jellyfinAuthenticator = jellyfinAuthenticator;
		_navigationService = navigationService;
		_setupNavigationService = setupNavigationServer;
		_navigationViewService = navigationViewService;
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
	public partial List<string> SavedUsers { get; set; } = [];

	public SavedServer? Server { get; private set; }


	[RelayCommand(CanExecute = nameof(CanLogin))]
	public async Task Login()
	{
		if(Server is null)
		{
			return;
		}

		var authenticated = await _jellyfinAuthenticator.Authenticate(Server.GetServerUrl(), Username, Password);

		if (!authenticated)
		{
			return;
		}

		if (KeepMeLoggedIn)
		{
			var users = _settingsService.ReadSetting(SettingKeys.Users);
			var passwordBytes = ProtectedData.Protect(Encoding.UTF8.GetBytes(Password), _settingsService.GetEntropyBytes(), DataProtectionScope.CurrentUser);
			if (users.All(x => x.Username != Username && !ByteArraysEqual(x.Password, passwordBytes) && x.ServerId != Server.Id))
			{
				var user = new SavedUser
				{
					Username = Username,
					Password = passwordBytes,
					ServerId = Server.Id
				};
				users.Add(user);
				_settingsService.SaveSetting(SettingKeys.Users, users);
			}
		}

		_setupNavigationService.NavigateTo(typeof(ShellViewModel).FullName!);
	}

	public async Task OnNavigatedFrom()
	{
		_navigationService.NavigateTo(typeof(HomeViewModel).FullName!, new object());
	}

	public Task OnNavigatedTo(object parameter)
	{
		if(parameter is not SavedServer server)
		{
			return Task.CompletedTask;
		}

		SavedUsers = _settingsService.ReadSetting(SettingKeys.Users).Where(x => x.ServerId == server.Id).Select(x => x.Username).ToList();
		Server = server;
		return Task.CompletedTask;
	}

	static bool ByteArraysEqual(ReadOnlySpan<byte> a1, ReadOnlySpan<byte> a2)
	{
		return a1.SequenceEqual(a2);
	}
}
