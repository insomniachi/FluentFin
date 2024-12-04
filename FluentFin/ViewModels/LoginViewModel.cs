using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentFin.Contracts.Services;
using FluentFin.Core.Contracts.Services;
using FluentFin.Core.ViewModels;
using Microsoft.UI.Xaml.Controls;
using ReactiveUI;
using System.Reactive.Linq;

namespace FluentFin.ViewModels;

public partial class LoginViewModel : ObservableObject
{
	private readonly IJellyfinAuthenticationService _jellyfinAuthenticator;
	private readonly INavigationService _navigationService;
	private readonly INavigationViewService _navigationViewService;
	private readonly IJellyfinClient _jellyfinClient;

	public LoginViewModel(IMainWindowViewModel mainWindowViewModel,
						  IJellyfinAuthenticationService jellyfinAuthenticator,
						  INavigationService navigationService,
						  INavigationViewService navigationViewService,
						  IJellyfinClient jellyfinClient)
	{
		_jellyfinAuthenticator = jellyfinAuthenticator;
		_navigationService = navigationService;
		_navigationViewService = navigationViewService;
		_jellyfinClient = jellyfinClient;

		this.WhenAnyValue(x => x.ServerUrl, x => x.Username, x => x.Password)
			.Select(x => !string.IsNullOrEmpty(x.Item1) && !string.IsNullOrEmpty(x.Item2) && !string.IsNullOrEmpty(x.Item3))
			.Subscribe(validDetails => CanLogin = validDetails);
	}


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

	[RelayCommand(CanExecute = nameof(CanLogin))]
	public async Task Login()
	{
		var authenticated = await _jellyfinAuthenticator.Authenticate(ServerUrl, Username, Password);

		if (authenticated)
		{
			var libarariesItem = new NavigationViewItem() { Content = "Libraries", Icon = new SymbolIcon { Symbol = Symbol.Library }, SelectsOnInvoked = false };
			await foreach (var item in _jellyfinClient.GetUserLibraries())
			{
				libarariesItem.MenuItems.Add(new NavigationViewItem
				{
					Content = item.Name,
				});
			}

			_navigationViewService.MenuItems?.Add(libarariesItem);

			_navigationService.NavigateTo(typeof(HomeViewModel).FullName!, new object());
		}
	}

}
