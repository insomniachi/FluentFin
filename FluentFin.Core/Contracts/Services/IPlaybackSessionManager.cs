using Jellyfin.Sdk.Generated.Models;

namespace FluentFin.Core.Contracts.Services;

/// <summary>
/// Owns the server-side lifecycle (start, progress, stop) of every playback session created by this client,
/// so that the matching Jellyfin transcoding job is always released.
/// </summary>
public interface IPlaybackSessionManager
{
	/// <summary>
	/// Registers a playback session as soon as the media URL has been obtained, independently of progress reports.
	/// </summary>
	PlaybackSession Begin(Guid itemId, MediaResponse media);

	Task ReportStartAsync(PlaybackSession session, PlaybackState state);

	Task ReportProgressAsync(PlaybackSession session, PlaybackState state);

	/// <summary>
	/// Reports the session as stopped. Safe to call any number of times and from any thread, only the first call sends requests.
	/// Never throws. When no position is known, or <paramref name="updateWatchState"/> is false, the report is sent as failed
	/// so that the server ends the playback session without touching the resume point or the played state.
	/// </summary>
	Task StopAsync(PlaybackSession? session, long? positionTicks = null, bool updateWatchState = true);

	/// <summary>
	/// Stops every session that is still active, waiting at most <paramref name="timeout"/>.
	/// </summary>
	Task StopAllAsync(TimeSpan timeout);
}

public record PlaybackState(long PositionTicks, bool IsPaused, bool IsMuted, int? AudioStreamIndex, int? SubtitleStreamIndex);

public sealed class PlaybackSession(Guid itemId, string playSessionId, string mediaSourceId, PlaybackProgressInfo_PlayMethod playMethod)
{
	private readonly object _lock = new();
	private TaskCompletionSource? _stopped;

	public Guid ItemId { get; } = itemId;
	public string PlaySessionId { get; } = playSessionId;
	public string MediaSourceId { get; } = mediaSourceId;
	public PlaybackProgressInfo_PlayMethod PlayMethod { get; } = playMethod;
	public long PositionTicks { get; internal set; }

	/// <summary>
	/// Optional way to read the live position when the session is stopped without an explicit position (e.g. on shutdown).
	/// </summary>
	public Func<long?>? PositionProvider { get; set; }

	public bool IsStopping
	{
		get { lock (_lock) { return _stopped is not null; } }
	}

	/// <summary>
	/// Marks the session as stopping. Returns true for the first caller only; later callers get the task of the first one.
	/// </summary>
	internal bool TryBeginStop(out Task stopped)
	{
		lock (_lock)
		{
			if (_stopped is not null)
			{
				stopped = _stopped.Task;
				return false;
			}

			_stopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			stopped = _stopped.Task;
			return true;
		}
	}

	internal void CompleteStop() => _stopped?.TrySetResult();

	internal SemaphoreSlim Gate { get; } = new(1, 1);
}
