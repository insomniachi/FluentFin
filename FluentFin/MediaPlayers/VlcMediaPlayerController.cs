using System.Reactive;
using System.Reactive.Linq;
using FluentFin.Core.Contracts.Services;
using LibVLCSharp.Shared;
using Microsoft.UI.Xaml.Controls;
using ReactiveMarbles.ObservableEvents;

namespace FluentFin.MediaPlayers
{
	internal sealed partial class VlcMediaPlayerController : IMediaPlayerController
	{
		private readonly LibVLC _libVLC;
		private readonly MediaPlayer _mp;
		private readonly Button _audioTracksButton;
		private Media? _media;
		private int _defaultAudioIndex;

		public VlcMediaPlayerController(IVideoView view, string[] swapChainOptions, Button audioTracksButton)
		{
			_libVLC = new LibVLC(enableDebugLogs: false, swapChainOptions);
			_mp = new MediaPlayer(_libVLC);
			_audioTracksButton = audioTracksButton;
			view.MediaPlayer = _mp;

			PositionChanged.Subscribe(p => Position = p);
		}

		public void OpenExternalSubtitleTrack(string url) => _mp.AddSlave(MediaSlaveType.Subtitle, url, true);
		public void OpenInternalSubtitleTrack(int trackIndex, int subtitleIndex) => _mp.SetSpu(trackIndex);
		public void DisableSubtitles() => _mp.SetSpu(-1);
		public void OpenAudioTrack(int index) => _mp.SetAudioTrack(index);
		public void Pause() => _mp.Pause();
		public bool Play() => _mp.Play();
		public bool Play(Uri uri, int defaultAudioIndex = 0)
		{
			if (_media is not null)
			{
				_media.ParsedChanged -= MediaParsed;
			}

			_media = new Media(_libVLC, uri);
			_defaultAudioIndex = defaultAudioIndex;
			_media.ParsedChanged += MediaParsed;
			return _mp.Play(_media);
		}

		public void Stop() => _mp.Stop();
		public void SeekTo(TimeSpan time) => _mp.SeekTo(time);
		public void Dispose() => _mp.Dispose();
		public IEnumerable<Core.Contracts.Services.AudioTrack> GetAudioTracks() => _mp.Media?.Tracks.Where(x => x.TrackType == TrackType.Audio).Select(x => new Core.Contracts.Services.AudioTrack(x.Id, x.Language, x.Description)) ?? [];

		public MediaPlayerState State => ConvertState(_mp.State);
		public TimeSpan Position { get; private set; }
		public bool IsPlaying => _mp.IsPlaying;
		public bool IsMuted => _mp.Mute;
		public int? SubtitleTrackIndex => _mp.Spu == -1 ? null : _mp.Spu;
		public int? AudioTrackIndex => _mp.AudioTrack == -1 ? null : _mp.AudioTrack;
		public double Volume
		{
			get => _mp.Volume;
			set => _mp.Volume = (int)value;
		}

		public IObservable<TimeSpan> DurationChanged => _mp.Events().LengthChanged.Select(e => TimeSpan.FromMilliseconds(e.Length));
		public IObservable<TimeSpan> PositionChanged => _mp.Events().TimeChanged.Select(e => TimeSpan.FromMilliseconds(e.Time));
		public IObservable<Unit> Playing => _mp.Events().Playing.Select(_ => Unit.Default);
		public IObservable<Unit> Paused => _mp.Events().Paused.Select(_ => Unit.Default);
		public IObservable<Unit> Ended => _mp.Events().EndReached.Select(_ => Unit.Default);
		public IObservable<Unit> Errored => _mp.Events().EncounteredError.Select(_ => Unit.Default);
		public IObservable<Unit> Stopped => _mp.Events().Stopped.Select(_ => Unit.Default);
		public IObservable<Unit> MediaLoaded => _mp.Events().MediaChanged.Select(_ => Unit.Default);
		public IObservable<double> VolumeChanged => _mp.Events().VolumeChanged.Select(e => e.Volume * 100d);
		public IObservable<string> SubtitleText { get; } = Observable.Empty<string>();

		private void MediaParsed(object? sender, MediaParsedChangedEventArgs e)
		{
			if (e.ParsedStatus == MediaParsedStatus.Done)
			{
				_audioTracksButton.DispatcherQueue.TryEnqueue(() =>
				{
					_audioTracksButton.Flyout = Converters.Converters.GetAudiosFlyout(this, _defaultAudioIndex);
				});
			}
		}

		private static MediaPlayerState ConvertState(VLCState state)
		{
			return state switch
			{
				VLCState.Opening => MediaPlayerState.Opening,
				VLCState.Playing => MediaPlayerState.Playing,
				VLCState.Paused => MediaPlayerState.Paused,
				VLCState.Stopped => MediaPlayerState.Stopped,
				VLCState.Ended => MediaPlayerState.Ended,
				VLCState.Error => MediaPlayerState.Error,
				_ => MediaPlayerState.Error,
			};
		}
	}
}
