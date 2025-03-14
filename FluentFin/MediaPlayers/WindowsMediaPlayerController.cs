using DynamicData;
using Microsoft.UI.Xaml.Controls;
using ReactiveMarbles.ObservableEvents;
using System.Reactive;
using System.Reactive.Linq;
using Windows.Media.Core;
using Windows.Media.Playback;

namespace FluentFin.MediaPlayers
{
    public sealed partial class WindowsMediaPlayerController : IMediaPlayerController
    {
        private readonly MediaPlayer _mp = new();
        private MediaPlaybackItem? _mediaItem;

        public WindowsMediaPlayerController(MediaPlayerElement element)
        {
            element.AreTransportControlsEnabled = false;
            element.SetMediaPlayer(_mp);
        }

        public MediaPlayerState State => ConvertState(_mp.CurrentState);
        public TimeSpan Position => _mp.Position;
        public bool IsPlaying => _mp.CurrentState is Windows.Media.Playback.MediaPlayerState.Playing;
        public bool IsMuted => _mp.IsMuted;
        public int? SubtitleTrackIndex => null;
        public int? AudioTrackIndex => null;
        public double Volume 
        {
            get => _mp.Volume;
            set => _mp.Volume = value;
        }


        public IObservable<TimeSpan> DurationChanged => _mp.PlaybackSession.Events().NaturalDurationChanged.Select(_ => _mp.NaturalDuration);
        public IObservable<TimeSpan> PositionChanged => _mp.PlaybackSession.Events().PositionChanged.Select(_ => _mp.Position);
        public IObservable<Unit> Playing => _mp.PlaybackSession.Events().PlaybackStateChanged.Where(_ => _mp.CurrentState is Windows.Media.Playback.MediaPlayerState.Playing).Select(_ => Unit.Default);
        public IObservable<Unit> Paused => _mp.PlaybackSession.Events().PlaybackStateChanged.Where(_ => _mp.CurrentState is Windows.Media.Playback.MediaPlayerState.Paused).Select(_ => Unit.Default);
        public IObservable<Unit> Ended => _mp.Events().MediaEnded.Select(_ => Unit.Default);
        public IObservable<Unit> Errored => _mp.Events().MediaFailed.Select(_ => Unit.Default); 
        public IObservable<Unit> Stopped => _mp.PlaybackSession.Events().PlaybackStateChanged.Where(_ => _mp.CurrentState is Windows.Media.Playback.MediaPlayerState.Stopped).Select(_ => Unit.Default);
        public IObservable<double> VolumeChanged => _mp.Events().VolumeChanged.Select(_ => _mp.Volume);
        public IObservable<string> SubtitleText { get; } = Observable.Empty<string>();

        public void Dispose() => _mp.Dispose();

        public IEnumerable<AudioTrack> GetAudioTracks()
        {
            if(_mediaItem is null)
            {
                return [];
            }

            return _mediaItem.AudioTracks.Select(x => new AudioTrack(_mediaItem.AudioTracks.IndexOf(x), x.Language, x.Label));
        }

        public void OpenAudioTrack(int index)
        {
            if(_mediaItem is null)
            {
                return;
            }

            _mediaItem.AudioTracks.SelectedIndex = index;
        }

        public void OpenExternalSubtitleTrack(string url)
        {
            if(_mediaItem is null)
            {
                return;
            }

            var track = TimedTextSource.CreateFromUri(new Uri(url));
            _mediaItem.Source.ExternalTimedTextSources.Add(track);
            track.Resolved += Track_Resolved;
        }

        public void OpenInternalSubtitleTrack(int index)
        {
            if(_mediaItem is null or { TimedMetadataTracks: null })
            {
                return;
            }

            var timedMetadataTracks = _mediaItem.TimedMetadataTracks;
            if (index >= 0 && index < timedMetadataTracks.Count)
            {
                foreach (var track in timedMetadataTracks.Index())
                {
                    _mediaItem.TimedMetadataTracks.SetPresentationMode((uint)track.Index, TimedMetadataTrackPresentationMode.Disabled);
                }

                _mediaItem.TimedMetadataTracks.SetPresentationMode((uint)index, TimedMetadataTrackPresentationMode.PlatformPresented);
            }
        }

        public void Pause() => _mp.Pause();
        
        public bool Play()
        {
            _mp.Play();
            return true;
        }

        public bool Play(Uri uri, int defaultAudioIndex = 0)
        {
            var source = MediaSource.CreateFromUri(uri);
            _mediaItem = new MediaPlaybackItem(source);
            _mp.Source = _mediaItem;
            _mp.Play();
            return true;
        }

        public void SeekTo(TimeSpan timeSpan) => _mp.Position = timeSpan;
        public void Stop() => _mp.Pause();

        private static MediaPlayerState ConvertState(Windows.Media.Playback.MediaPlayerState state)
        {
            return state switch
            {
                Windows.Media.Playback.MediaPlayerState.Opening => MediaPlayerState.Opening,
                Windows.Media.Playback.MediaPlayerState.Playing => MediaPlayerState.Playing,
                Windows.Media.Playback.MediaPlayerState.Paused => MediaPlayerState.Paused,
                Windows.Media.Playback.MediaPlayerState.Stopped => MediaPlayerState.Stopped,
                _ => MediaPlayerState.Error,
            };
        }

        private void Track_Resolved(TimedTextSource sender, TimedTextSourceResolveResultEventArgs args)
        {
            var index = args.Tracks[0].PlaybackItem.TimedMetadataTracks.IndexOf(args.Tracks[0]);
            args.Tracks[0].PlaybackItem.TimedMetadataTracks.SetPresentationMode((uint)index, TimedMetadataTrackPresentationMode.PlatformPresented);
            sender.Resolved -= Track_Resolved;
        }
    }
}
