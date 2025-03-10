using FluentFin.Activation;
using FluentFin.Contracts.Services;
using FluentFin.Core;
using FluentFin.Core.Contracts.Services;
using FluentFin.Core.Services;
using FluentFin.Core.Settings;
using FluentFin.Core.ViewModels;
using FluentFin.Core.WebSockets;
using FluentFin.Dialogs;
using FluentFin.Dialogs.UserInput;
using FluentFin.Dialogs.ViewModels;
using FluentFin.Dialogs.Views;
using FluentFin.Helpers;
using FluentFin.Plugins.Playback_Reporting.ViewModels;
using FluentFin.Services;
using FluentFin.ViewModels;
using FluentFin.Views;
using Jellyfin.Sdk.Generated.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Serilog;
using System.Reactive.Subjects;
using System.Reflection;
using Windows.ApplicationModel;
using Windows.Storage;

namespace FluentFin;

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
			var knownFolders = new Core.KnownFolders();
			Log.Logger = new LoggerConfiguration()
				.Enrich.FromLogContext()
				.WriteTo.File(Path.Combine(knownFolders.Logs, "log.txt"), Serilog.Events.LogEventLevel.Debug, rollingInterval: RollingInterval.Day)
				.CreateLogger();
			services.AddLogging(builder => builder.AddSerilog());

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
			services.AddNavigationViewNavigation(NavigationRegions.PlaybackReporting);

			// Core Services
			services.AddTransient<LoginViewModel>();
			services.AddSingleton<ILocalSettingsService, LocalSettingsService>();
			services.AddSingleton<IJellyfinAuthenticationService, JellyfinAuthenticationService>();
			services.AddSingleton<IJellyfinClient, JellyfinClient>();
			services.AddSingleton<ISettings, Settings>();
			services.AddSingleton(knownFolders);
			services.AddSingleton<Subject<IInboundSocketMessage>>();
			services.AddSingleton<IObservable<IInboundSocketMessage>>(sp => sp.GetRequiredService<Subject<IInboundSocketMessage>>());
			services.AddSingleton<IObserver<IInboundSocketMessage>>(sp => sp.GetRequiredService<Subject<IInboundSocketMessage>>());


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
			services.AddTransient<EpisodeViewModel>();
			services.AddTransient<SeriesViewModel>();
			services.AddTransient<SeasonViewModel>();
			services.AddTransient<JellyfinSettingsViewModel>();
			services.AddTransient<SettingsViewModel>();
			services.AddTransient<TrickplayViewModel>();
			services.AddTransient<SelectServerViewModel>();
			services.AddTransient<MediaSegmentsEditorViewModel>();

			// dashboard view models
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
			services.AddTransient<PlaybackTranscodingViewModel>();
			services.AddTransient<ActivitiesViewModel>();
			services.AddTransient<LibrariesLandingPageViewModel>();
			services.AddTransient<ScheduledTasksViewModel>();

			// playback report view models
			services.AddTransient<PlaybackReportingDashboardViewModel>();
			services.AddTransient<UsersReportViewModel>();
			services.AddTransient<PlaybackReportViewModel>();
			services.AddTransient<BreakdownReportViewModel>();
			services.AddTransient<UsageReportViewModel>();
			services.AddTransient<SessionDurationReportViewModel>();

			// Dialogs
			services.AddDialog<EditMetadataViewModel, EditMetadataDialog>();
			services.AddDialog<EditImagesViewModel, EditImagesDialog>();
			services.AddDialog<EditSubtitlesViewModel, EditSubtitlesDialog>();
			services.AddDialog<IdentifyViewModel, IdentifyDialog>();
			services.AddDialog<MediaInfoViewModel, MediaInfoDialog>();
			services.AddDialog<RefreshMetadataViewModel, RefreshMetadataDialog>();
			services.AddDialog<AccessSchedulePickerViewModel, AccessSchedulePickerDialog>();
			services.AddDialog<AddUserViewModel, AddUserDialog>();
			services.AddDialog<ManageLibraryViewModel, ManageLibraryDialog>();
			services.AddDialog<StringPickerViewModel, StringPickerDialog>();
			services.AddDialog<QuickConnectViewModel, QuickConnectDialog>();

			// Pickers
			services.AddTransient<IUserInput<AccessSchedule>, AccessScheduleUserInput>();
			services.AddTransient<IUserInput<string>, StringUserInput>();
			services.AddTransient<IServerFolderPicker, ServerFolderInput>();

			services.AddTransient<ShellPage>();

			// Configuration

			if(!IsPackaged())
			{
				services.AddHostedService<WindowsUpdateService>();
			}
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

		Locator.SetServiceProvider(Host.Services);

		base.OnLaunched(args);

		MainWindow.Closed += MainWindow_Closed;
		
		StartFlyleaf();

		await GetService<IActivationService>().ActivateAsync(args);
	}

	private static bool IsPackaged()
	{
		try
		{
			_ = Package.Current;
			return true;
		}
		catch
		{
			return false;
		}
	}

	private async void MainWindow_Closed(object sender, WindowEventArgs args)
	{
		await Host.StopAsync();
		await GetService<IJellyfinClient>().Stop();
	}

	private static void StartFlyleaf()
	{
		var location = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!, "FFMpeg");
		FlyleafLib.Engine.Start(new FlyleafLib.EngineConfig()
		{
			FFmpegDevices = false,    // Prevents loading avdevice/avfilter dll files. Enable it only if you plan to use dshow/gdigrab etc.
			FFmpegPath = location ?? "FFmpeg",
			FFmpegLogLevel = Flyleaf.FFmpeg.LogLevel.Trace,
			LogLevel = FlyleafLib.LogLevel.Trace,
			UIRefresh = true,    // Required for Activity, BufferedDuration, Stats in combination with Config.Player.Stats = true
			UIRefreshInterval = 250,      // How often (in ms) to notify the UI
			UICurTimePerSecond = true,  // Whether to notify UI for CurTime only when it's second changed or by UIRefreshInterval
			LogOutput = "Test.txt"
		});
	}
}
