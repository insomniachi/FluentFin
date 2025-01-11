namespace FluentFin.Core.WebSockets;

public enum SessionMessageType
{
	// Server -> Client
	ForceKeepAlive,
	GeneralCommand,
	UserDataChanged,
	Sessions,
	Play,
	SyncPlayCommand,
	SyncPlayGroupUpdate,
	Playstate,
	RestartRequired,
	ServerShuttingDown,
	ServerRestarting,
	LibraryChanged,
	UserDeleted,
	UserUpdated,
	SeriesTimerCreated,
	TimerCreated,
	SeriesTimerCancelled,
	TimerCancelled,
	RefreshProgress,
	ScheduledTaskEnded,
	PackageInstallationCancelled,
	PackageInstallationFailed,
	PackageInstallationCompleted,
	PackageInstalling,
	PackageUninstalled,
	ActivityLogEntry,
	ScheduledTasksInfo,

	// Client -> Server
	ActivityLogEntryStart,
	ActivityLogEntryStop,
	SessionsStart,
	SessionsStop,
	ScheduledTasksInfoStart,
	ScheduledTasksInfoStop,

	// Shared
	KeepAlive,
}
