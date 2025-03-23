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
    public static void TogglePlayPlause(this IMediaPlayerController controller)
    {
        if(controller is null)
        {
            return;
        }

        if(controller.State is MediaPlayerState.Playing)
        {
            controller.Pause();
        }
        else if(controller.State is MediaPlayerState.Paused)
        {
            controller.Play();
        }
    }

    public static void SeekForward(this IMediaPlayerController controller, TimeSpan ts)
    {
        controller.SeekTo(controller.Position + ts);
    }

    public static void SeekBackward(this IMediaPlayerController controller, TimeSpan ts)
    {
        controller.SeekTo(controller.Position - ts);
    }
}
