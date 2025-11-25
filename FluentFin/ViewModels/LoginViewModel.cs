using System.Reactive.Disposables;
using System.Reactive.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentFin.Contracts.Services;
using FluentFin.Contracts.ViewModels;
using FluentFin.Core;
using FluentFin.Core.Contracts.Services;
using FluentFin.Core.Services;
using FluentFin.Core.Settings;
using FluentFin.Core.ViewModels;
using FluentFin.Helpers;
using FluentFin.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using ReactiveUI;

namespace FluentFin.ViewModels;

public partial class LoginViewModel(IJellyfinAuthenticationService jellyfinAuthenticator,
									[FromKeyedServices(NavigationRegions.InitialSetup)] INavigationService navigationService,
									ILocalSettingsService settingsService,
									IContentDialogService contentDialogService) : ObservableObject, INavigationAware
{
	private CompositeDisposable? _disposable;

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

	[ObservableProperty]
	public partial Uri? SplashScreen { get; set; }


	[RelayCommand(CanExecute = nameof(CanLogin))]
	public async Task Login()
	{
		if (Server is null)
		{
			return;
		}

		var success = await jellyfinAuthenticator.Authenticate(Server, Username, Password, KeepMeLoggedIn);

		if (!success)
		{
			await contentDialogService.ShowMessage("Authentication Failed", "Incorrect username or password");
		}
	}

	[RelayCommand]
	private async Task QuickConnect()
	{
		if (Server is null)
		{
			return;
		}

		var response = await jellyfinAuthenticator.GetQuickConnectCode(Server);

		if (response is null)
		{
			return;
		}

		_disposable = [];


		var dialog = new ContentDialog
		{
			XamlRoot = App.MainWindow.Content.XamlRoot,
			Title = "Quick connect",
			Content = $"Enter code {response.Code} to login",
			PrimaryButtonText = "OK",
			DefaultButton = ContentDialogButton.Primary
		};


		Observable.Timer(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5))
			.Where(_ => Server is not null)
			.SelectMany(_ => jellyfinAuthenticator.CheckQuickConnectStatus(Server, response))
			.WhereNotNull()
			.ObserveOn(RxApp.MainThreadScheduler)
			.Subscribe(result =>
			{
				if (result.Authenticated == true)
				{
					_disposable?.Dispose();
					dialog.Hide();
					var isAuthenticated = jellyfinAuthenticator.Authenticate(Server, result);
				}
			})
			.DisposeWith(_disposable);

		await dialog.ShowAsync();
	}


	[RelayCommand]
	private void SwitchServer()
	{
		navigationService.NavigateTo<SelectServerViewModel>();
	}

	public Task OnNavigatedFrom() => Task.CompletedTask;

	public Task OnNavigatedTo(object parameter)
	{
		if (parameter is not SavedServer server)
		{
			return Task.CompletedTask;
		}

		this.WhenAnyValue(x => x.Username)
			.Select(x => !string.IsNullOrEmpty(x) && Server is not null)
			.Subscribe(validDetails => CanLogin = validDetails);

		Server = server;
		SplashScreen = JellyfinAuthenticationService.GetSplashScreen(Server.GetServerUrl());

		return Task.CompletedTask;
	}

	public string Unprotect(byte[] protectedBytes) => protectedBytes.Unprotect(settingsService.GetEntropyBytes());
}
