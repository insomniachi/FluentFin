using System.Reactive;

namespace FluentFin.Core.Contracts.Services;

public interface IMediaPlayerController : IDisposable
{
	bool Play();
	bool Play(Uri uri, int defaultAudioIndex = 0);
	void Pause();
	void Stop();
	void OpenExternalSubtitleTrack(string url);
	void OpenInternalSubtitleTrack(int trackIndex, int subtitleIndex);
	void DisableSubtitles();
	void OpenAudioTrack(int index);
	void SeekTo(TimeSpan timeSpan);
	IEnumerable<AudioTrack> GetAudioTracks();

	MediaPlayerState State { get; }
	TimeSpan Position { get; }
	bool IsPlaying { get; }
	bool IsMuted { get; }
	int? SubtitleTrackIndex { get; }
	int? AudioTrackIndex { get; }
	double Volume { get; set; }

	IObservable<TimeSpan> DurationChanged { get; }
	IObservable<TimeSpan> PositionChanged { get; }
	IObservable<Unit> Playing { get; }
	IObservable<Unit> Paused { get; }
	IObservable<Unit> Ended { get; }
	IObservable<Unit> Errored { get; }
	IObservable<Unit> Stopped { get; }
	IObservable<Unit> MediaLoaded { get; }
	IObservable<double> VolumeChanged { get; }
	IObservable<string> SubtitleText { get; }
}

public enum MediaPlayerState
{
	Opening,
	Playing,
	Paused,
	Stopped,
	Ended,
	Error
}

public enum MediaPlayerType
{
	Vlc,
	Flyleaf,
	WindowsMediaPlayer
}

public record struct AudioTrack(int Id, string? Language, string? Name);

public static class MediaPlayerControllerExtensions
{
	public static async ValueTask TogglePlayPlause(this IMediaPlayerController controller, IJellyfinClient client)
	{
		if (controller is null)
		{
			return;
		}

		if (controller.State is MediaPlayerState.Playing)
		{
			await (client?.SignalPauseForSyncPlay() ?? Task.CompletedTask);
			controller.Pause();
		}
		else if (controller.State is MediaPlayerState.Paused)
		{
			await (client?.SignalUnpauseForSyncPlay() ?? Task.CompletedTask);
			controller.Play();
		}
	}

	public static async ValueTask SeekForward(this IMediaPlayerController controller, IJellyfinClient client, TimeSpan ts)
	{
		if(controller is null)
		{
			return;
		}

		var newTime = controller.Position + ts;

		await (client?.SignalSeekForSyncPlay(ts) ?? Task.CompletedTask);
		controller.SeekTo(newTime);
	}

	public static async ValueTask SeekBackward(this IMediaPlayerController controller, IJellyfinClient client, TimeSpan ts)
	{
		if (controller is null)
		{
			return;
		}

		var newTime = controller.Position - ts;

		await (client?.SignalSeekForSyncPlay(newTime) ?? Task.CompletedTask);
		controller.SeekTo(newTime);
	}

	public static async ValueTask SeekTo(this IMediaPlayerController controller, IJellyfinClient client, TimeSpan ts)
	{
		if (controller is null)
		{
			return;
		}

		await (client?.SignalSeekForSyncPlay(ts) ?? Task.CompletedTask);
		controller.SeekTo(ts);
	}
}
