using FluentFin.Core.Contracts.Services;
using System.Reactive;

namespace FluentFin.MediaPlayers;

public interface IMediaPlayerController : IDisposable
{
    bool Play();
    bool Play(Uri uri, int defaultAudioIndex = 0);
    void Pause();
    void Stop();
    void OpenExternalSubtitleTrack(string url);
    void OpenInternalSubtitleTrack(int index);
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

public record struct AudioTrack(int Id, string? Language, string? Name);
