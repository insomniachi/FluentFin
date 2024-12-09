using FluentFin.Activation;
using FluentFin.Contracts.Services;
using FluentFin.Core;
using FluentFin.Core.Contracts.Services;
using FluentFin.Core.Services;
using FluentFin.Core.Settings;
using FluentFin.Core.ViewModels;
using FluentFin.Helpers;
using FluentFin.Services;
using FluentFin.ViewModels;
using FluentFin.Views;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using System.ComponentModel;
using System.Text.Json;

namespace FluentFin;

// To learn more about WinUI 3, see https://docs.microsoft.com/windows/apps/winui/winui3/.
public partial class App : Application
{
    public IHost Host { get; }

    public static T GetService<T>()
        where T : class
    {
        if ((Current as App)!.Host.Services.GetService(typeof(T)) is not T service)
        {
            throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices within App.xaml.cs.");
        }

        return service;
    }

    public static WindowEx MainWindow { get; } = new MainWindow();

	public static GlobalCommands Commands { get; private set; } = null!;

    public App()
    {
		InitializeComponent();

        Host = Microsoft.Extensions.Hosting.Host.
        CreateDefaultBuilder().
        UseContentRoot(AppContext.BaseDirectory).
        ConfigureServices((context, services) =>
        {
            // Default Activation Handler
            services.AddTransient<ActivationHandler<LaunchActivatedEventArgs>, DefaultActivationHandler>();

            // Other Activation Handlers

            // Services
            services.AddSingleton<INavigationViewService, NavigationViewService>();

            services.AddSingleton<IActivationService, ActivationService>();
            services.AddSingleton<IPageService, PageService>();
            services.AddSingleton<INavigationService, NavigationService>();
			services.AddSingleton<INavigationServiceCore>(sp => sp.GetRequiredService<INavigationService>());

			// Core Services
			services.AddTransient<LoginViewModel>();
			services.AddSingleton<ILocalSettingsService, LocalSettingsService>();
			services.AddSingleton<IJellyfinAuthenticationService, JellyfinAuthentionService>();
			services.AddSingleton<IJellyfinClient, JellyfinClient>();
			services.AddSingleton<ISettings, Settings>();
			services.AddSingleton<KnownFolders>();
			services.AddSingleton<GlobalCommands>();

            // Views and ViewModels
			services.AddSingleton<IMainWindowViewModel, MainViewModel>();
			services.AddSingleton<ITitleBarViewModel, TitleBarViewModel>();
			services.AddTransient<HomeViewModel>();
			services.AddTransient<LibraryViewModel>();
			services.AddTransient<VideoPlayerViewModel>();
			services.AddTransient<MovieViewModel>();

            services.AddTransient<ShellPage>();
            services.AddTransient<ShellViewModel>();

            // Configuration
        }).
        Build();

        UnhandledException += App_UnhandledException;
    }

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
		GetService<ILogger<App>>().LogError(e.Exception, e.Message);
    }

    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);

		MainWindow.Closed += MainWindow_Closed;


		Commands = GetService<GlobalCommands>();
        await GetService<IActivationService>().ActivateAsync(args);
    }

	private async void MainWindow_Closed(object sender, WindowEventArgs args)
	{
		await GetService<IJellyfinClient>().Stop();
	}
}
