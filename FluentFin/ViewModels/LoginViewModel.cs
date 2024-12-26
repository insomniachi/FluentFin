using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentFin.Contracts.Services;
using FluentFin.Core.Contracts.Services;
using FluentFin.Core.Settings;
using FluentFin.Core.ViewModels;
using ReactiveUI;
using System.ComponentModel.DataAnnotations;
using System.Reactive.Linq;
using System.Security.Cryptography;
using System.Text;

namespace FluentFin.ViewModels;

public partial class LoginViewModel : ObservableObject
{
	private readonly IJellyfinAuthenticationService _jellyfinAuthenticator;
	private readonly INavigationService _navigationService;
	private readonly INavigationViewService _navigationViewService;
	private readonly ILocalSettingsService _settingsService;

	public LoginViewModel(IMainWindowViewModel mainWindowViewModel,
						  IJellyfinAuthenticationService jellyfinAuthenticator,
						  INavigationService navigationService,
						  INavigationViewService navigationViewService,
						  ILocalSettingsService settingsService)
	{
		_jellyfinAuthenticator = jellyfinAuthenticator;
		_navigationService = navigationService;
		_navigationViewService = navigationViewService;
		_settingsService = settingsService;

		this.WhenAnyValue(x => x.ServerUrl, x => x.Username, x => x.Password)
			.Select(x => !string.IsNullOrEmpty(x.Item1) && !string.IsNullOrEmpty(x.Item2) && !string.IsNullOrEmpty(x.Item3))
			.Subscribe(validDetails => CanLogin = validDetails);
	}


	//[ObservableProperty]
	//public partial string ServerUrl { get; set; } = "https://jellyfin.chaithram.xyz/jellyfin";

	[ObservableProperty]
	public partial string ServerUrl { get; set; } = "http://192.168.1.200:8096/jellyfin";

	[ObservableProperty]
	public partial string Username { get; set; } = "";

	[ObservableProperty]
	public partial string Password { get; set; } = "";

	[ObservableProperty]
	public partial bool KeepMeLoggedIn { get; set; }

	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(LoginCommand))]
	public partial bool CanLogin { get; set; }

	public async Task Initialize()
	{
		var serverSettings = _settingsService.ReadSetting(SettingKeys.ServerSettings);

		if(string.IsNullOrEmpty(serverSettings.ServerUrl) || string.IsNullOrEmpty(serverSettings.Username) || serverSettings.Password.Length == 0)
		{
			return;
		}

		ServerUrl = serverSettings.ServerUrl;
		Username = serverSettings.Username;

		try
		{
			Password = Encoding.UTF8.GetString(ProtectedData.Unprotect(serverSettings.Password, _settingsService.GetEntropyBytes(), DataProtectionScope.CurrentUser));
		}
		catch { }

		await Login();
	}

	[RelayCommand(CanExecute = nameof(CanLogin))]
	public async Task Login()
	{
		var authenticated = await _jellyfinAuthenticator.Authenticate(ServerUrl, Username, Password);

		if (authenticated)
		{
			if(KeepMeLoggedIn)
			{
				SaveLogin(ServerUrl, Username, Password);
			}

			Username = "";
			Password = "";
			KeepMeLoggedIn = false;

			await _navigationViewService.InitializeLibraries();
			_navigationService.NavigateTo(typeof(HomeViewModel).FullName!, new object());
		}
	}

	public void SaveLogin(string server, string username, string password)
	{
		try
		{
			var serverSettings = new ServerSettings
			{
				ServerUrl = server,
				Username = username,
				Password = ProtectedData.Protect(Encoding.UTF8.GetBytes(password), _settingsService.GetEntropyBytes(), DataProtectionScope.CurrentUser)
			};

			_settingsService.SaveSetting(SettingKeys.ServerSettings, serverSettings);
		}
		catch { }
	}
}
