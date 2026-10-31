using System.Collections.Concurrent;
using FluentFin.Core.Contracts.Services;
using Jellyfin.Sdk.Generated.Models;
using Microsoft.Extensions.Logging;

namespace FluentFin.Core.Services;

public class PlaybackSessionManager(IJellyfinClient jellyfinClient, ILogger<PlaybackSessionManager> logger) : IPlaybackSessionManager
{
	private readonly ConcurrentDictionary<PlaybackSession, byte> _active = new();

	public PlaybackSession Begin(Guid itemId, MediaResponse media)
	{
		var session = new PlaybackSession(itemId, media.PlaybackSessionId, media.MediaSourceId, media.PlayMethod);
		_active[session] = 0;

		if (_active.Count > 1)
		{
			logger.LogWarning("{Count} playback sessions are active at the same time: {PlaySessionIds}",
				_active.Count, string.Join(", ", _active.Keys.Select(x => x.PlaySessionId)));
		}

		return session;
	}

	public async Task ReportStartAsync(PlaybackSession session, PlaybackState state)
	{
		await session.Gate.WaitAsync();
		try
		{
			if (session.IsStopping)
			{
				return;
			}

			// the player has usually not started yet, this position is not stored as the last known one
			await jellyfinClient.Playing(new PlaybackStartInfo
			{
				ItemId = session.ItemId,
				MediaSourceId = session.MediaSourceId,
				PlaySessionId = session.PlaySessionId,
				PlayMethod = Convert<PlaybackStartInfo_PlayMethod>(session.PlayMethod),
				PositionTicks = state.PositionTicks,
				IsPaused = state.IsPaused,
				IsMuted = state.IsMuted,
				AudioStreamIndex = state.AudioStreamIndex,
				SubtitleStreamIndex = state.SubtitleStreamIndex,
				CanSeek = true,
			});
		}
		finally
		{
			session.Gate.Release();
		}
	}

	public async Task ReportProgressAsync(PlaybackSession session, PlaybackState state)
	{
		await session.Gate.WaitAsync();
		try
		{
			// a stop that is queued behind this report must not be followed by a late progress report
			if (session.IsStopping)
			{
				return;
			}

			session.PositionTicks = state.PositionTicks;
			await jellyfinClient.Progress(new PlaybackProgressInfo
			{
				ItemId = session.ItemId,
				MediaSourceId = session.MediaSourceId,
				PlaySessionId = session.PlaySessionId,
				PlayMethod = session.PlayMethod,
				PositionTicks = state.PositionTicks,
				IsPaused = state.IsPaused,
				IsMuted = state.IsMuted,
				AudioStreamIndex = state.AudioStreamIndex,
				SubtitleStreamIndex = state.SubtitleStreamIndex,
				CanSeek = true,
			});
		}
		finally
		{
			session.Gate.Release();
		}
	}

	public async Task StopAsync(PlaybackSession? session, long? positionTicks = null, bool updateWatchState = true)
	{
		if (session is null)
		{
			return;
		}

		if (!session.TryBeginStop(out var stopped))
		{
			await stopped;
			return;
		}

		try
		{
			var position = positionTicks ?? ReadPosition(session);

			// waits for an in-flight progress report so that the stop is always the last request of the session
			await session.Gate.WaitAsync();
			try
			{
				var reported = await jellyfinClient.Stop(new PlaybackStopInfo
				{
					ItemId = session.ItemId,
					MediaSourceId = session.MediaSourceId,
					PlaySessionId = session.PlaySessionId,
					PositionTicks = position ?? 0,
					// without a real position the server would reset the resume point (0) or mark the item as played (null),
					// a failed report still ends the playback session, but leaves the watch state alone
					Failed = !updateWatchState || position is null,
				});

				// the stop report is what makes the server kill the transcoding job, if it failed ask for it explicitly
				if (!reported && session.PlayMethod == PlaybackProgressInfo_PlayMethod.Transcode)
				{
					await jellyfinClient.StopEncoding(session.PlaySessionId);
				}
			}
			finally
			{
				session.Gate.Release();
			}
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Failed to stop playback session {PlaySessionId}", session.PlaySessionId);
		}
		finally
		{
			_active.TryRemove(session, out _);
			session.CompleteStop();
		}
	}

	public async Task StopAllAsync(TimeSpan timeout)
	{
		var stops = Task.WhenAll(_active.Keys.Select(x => StopAsync(x)));
		await Task.WhenAny(stops, Task.Delay(timeout));

		if (!stops.IsCompleted)
		{
			logger.LogWarning("Timed out while stopping playback sessions");
		}
	}

	private static long? ReadPosition(PlaybackSession session)
	{
		try
		{
			if (session.PositionProvider?.Invoke() is { } live)
			{
				return live;
			}
		}
		catch
		{
			// fall back to the last reported position
		}

		return session.PositionTicks;
	}

	private static T Convert<T>(PlaybackProgressInfo_PlayMethod method) where T : struct, Enum
		=> Enum.TryParse<T>(method.ToString(), out var result) ? result : default;
}
