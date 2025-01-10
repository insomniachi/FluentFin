using FluentFin.Activation;
using FluentFin.Contracts.Services;
using FluentFin.Core;
using FluentFin.Core.Contracts.Services;
using FluentFin.Core.Services;
using FluentFin.Core.Settings;
using FluentFin.Core.ViewModels;
using FluentFin.Dialogs;
using FluentFin.Dialogs.UserInput;
using FluentFin.Dialogs.ViewModels;
using FluentFin.Dialogs.Views;
using FluentFin.Helpers;
using FluentFin.Services;
using FluentFin.ViewModels;
using FluentFin.Views;
using Jellyfin.Sdk.Generated.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;

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

	public static T GetKeyedService<T>(object key)
	where T : class
	{
		if ((Current as App)!.Host.Services.GetKeyedService<T>(key) is not T service)
		{
			throw new ArgumentException($"{typeof(T)} needs to be registered in ConfigureServices within App.xaml.cs.");
		}

		return service;
	}

	public static WindowEx MainWindow { get; } = new MainWindow();

    public static GlobalCommands Commands { get; } = GetService<GlobalCommands>();

    public static DialogCommands Dialogs { get; } = GetService<DialogCommands>();


    public App()
    {
		InitializeComponent();

		Host = Microsoft.Extensions.Hosting.Host
        .CreateDefaultBuilder()
        .UseContentRoot(AppContext.BaseDirectory)
        .ConfigureServices((context, services) =>
        {
            // Default Activation Handler
            services.AddTransient<ActivationHandler<LaunchActivatedEventArgs>, DefaultActivationHandler>();

            // Other Activation Handlers

            // Services
            services.AddSingleton<IActivationService, ActivationService>();
            services.AddSingleton<IPageService, PageService>();
            services.AddSingleton<INavigationViewService, NavigationViewService>();
            services.AddSingleton<INavigationService, NavigationService>();
			services.AddSingleton((Func<IServiceProvider, INavigationServiceCore>)(sp => sp.GetRequiredService<INavigationService>()));
            services.AddTransient<IContentDialogService, ContentDialogService>();

			services.AddNavigationViewNavigation(NavigationRegions.Settings);
			services.AddFrameNavigation(NavigationRegions.UserEditor);
			services.AddFrameNavigation(NavigationRegions.InitialSetup);

			// Core Services
			services.AddTransient<LoginViewModel>();
			services.AddSingleton<ILocalSettingsService, LocalSettingsService>();
			services.AddSingleton<IJellyfinAuthenticationService, JellyfinAuthenticationService>();
			services.AddSingleton<IJellyfinClient, JellyfinClient>();
			services.AddSingleton<ISettings, Settings>();
			services.AddSingleton<KnownFolders>();

			// Commands
			services.AddSingleton<GlobalCommands>();
			services.AddSingleton<DialogCommands>();

			// Views and ViewModels
			services.AddTransient<ShellViewModel>();
			services.AddSingleton<IMainWindowViewModel, MainViewModel>();
			services.AddSingleton<ITitleBarViewModel, TitleBarViewModel>();
			services.AddTransient<HomeViewModel>();
			services.AddTransient<LibraryViewModel>();
			services.AddTransient<VideoPlayerViewModel>();
			services.AddTransient<MovieViewModel>();
			services.AddTransient<SeriesViewModel>();
			services.AddTransient<SeasonViewModel>();
			services.AddTransient<JellyfinSettingsViewModel>();
			services.AddTransient<SettingsViewModel>();
			services.AddTransient<TrickplayViewModel>();
			services.AddTransient<SelectServerViewModel>();

			services.AddTransient<DashboardViewModel>();
			services.AddTransient<GeneralSettingsViewModel>();
			services.AddTransient<UsersViewModel>();
			services.AddTransient<UserEditorViewModel>();
			services.AddTransient<UserProfileEditorViewModel>();
			services.AddTransient<UserAccessEditorViewModel>();
			services.AddTransient<UserParentalControlEditorViewModel>();
			services.AddTransient<UserPasswordEditorViewModel>();
			services.AddTransient<LibrariesSettingsViewModel>();
			services.AddTransient<LibrariesMetadataViewModel>();
			services.AddTransient<LibrariesNfoSettingsViewModel>();
			services.AddTransient<LibrariesDisplayViewModel>();
			services.AddTransient<PlaybackResumeViewModel>();
			services.AddTransient<PlaybackTrickplayViewModel>();

			// Dialogs
			services.AddDialog<EditMetadataViewModel, EditMetadataDialog>();
            services.AddDialog<EditImagesViewModel, EditImagesDialog>();
            services.AddDialog<EditSubtitlesViewModel, EditSubtitlesDialog>();
            services.AddDialog<IdentifyViewModel, IdentifyDialog>();
            services.AddDialog<MediaInfoViewModel, MediaInfoDialog>();
            services.AddDialog<RefreshMetadataViewModel, RefreshMetadataDialog>();
			services.AddDialog<AccessSchedulePickerViewModel, AccessSchedulePickerDialog>();

			services.AddDialog<AddUserViewModel, AddUserDialog>();

			// Pickers
			services.AddTransient<IUserInput<AccessSchedule>, AccessScheduleUserInput>();

			services.AddTransient<ShellPage>();

			// Configuration

			services.AddHostedService<WindowsUpdateService>();
        }).
        Build();

        UnhandledException += App_UnhandledException;
    }

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
		GetService<ILogger<App>>().LogError(e.Exception, "Unhandled exception");
        e.Handled = true;
    }

    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
		await Host.StartAsync();

		base.OnLaunched(args);

		MainWindow.Closed += MainWindow_Closed;
        StartFlyleaf();

        await GetService<IActivationService>().ActivateAsync(args);
    }

	private async void MainWindow_Closed(object sender, WindowEventArgs args)
	{
		await Host.StopAsync();
		await GetService<IJellyfinClient>().Stop();
	}

	private static void StartFlyleaf()
	{
		FlyleafLib.Engine.Start(new FlyleafLib.EngineConfig()
		{
			FFmpegDevices = false,    // Prevents loading avdevice/avfilter dll files. Enable it only if you plan to use dshow/gdigrab etc.

#if RELEASE
            FFmpegPath = @"FFmpeg",
            FFmpegLogLevel = Flyleaf.FFmpeg.LogLevel.Quiet,
            LogLevel = FlyleafLib.LogLevel.Quiet,

#else
			FFmpegLogLevel = Flyleaf.FFmpeg.LogLevel.Warn,
			LogLevel = FlyleafLib.LogLevel.Debug,
			LogOutput = ":debug",
			FFmpegPath = @"E:\FFmpeg",
#endif
			UIRefresh = false,    // Required for Activity, BufferedDuration, Stats in combination with Config.Player.Stats = true
			UIRefreshInterval = 250,      // How often (in ms) to notify the UI
			UICurTimePerSecond = true,     // Whether to notify UI for CurTime only when it's second changed or by UIRefreshInterval
		});
	}
}
