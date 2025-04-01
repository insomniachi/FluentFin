using System.Reactive;
using System.Reactive.Linq;
using DynamicData;
using FluentFin.Core.Contracts.Services;
using Microsoft.UI.Xaml.Controls;
using ReactiveMarbles.ObservableEvents;
using Windows.Media.Core;

namespace FluentFin.MediaPlayers
{
	public static class MPExtensions
	{
		public static T SafeGetValue<T>(this Windows.Media.Playback.MediaPlayer mp, Func<Windows.Media.Playback.MediaPlayer, T> getter, T defaultValue)
		{
			try
			{
				return getter(mp);
			}
			catch
			{
				return defaultValue;
			}
		}
	}

	public sealed partial class WindowsMediaPlayerController : IMediaPlayerController
	{
		private readonly Windows.Media.Playback.MediaPlayer _mp = new() { AutoPlay = false };
		private Windows.Media.Playback.MediaPlaybackItem? _mediaItem;
		private readonly Button _audioTracksButton;

		public WindowsMediaPlayerController(MediaPlayerElement element, Button audioTracksButton)
		{
			_audioTracksButton = audioTracksButton;
			element.AreTransportControlsEnabled = false;
			element.SetMediaPlayer(_mp);
		}

		public MediaPlayerState State => ConvertState(_mp.SafeGetValue(mp => mp.CurrentState, Windows.Media.Playback.MediaPlayerState.Closed));
		public TimeSpan Position => _mp.SafeGetValue(mp => mp.Position, TimeSpan.Zero);
		public bool IsPlaying => _mp.SafeGetValue(mp => mp.CurrentState, Windows.Media.Playback.MediaPlayerState.Closed) is Windows.Media.Playback.MediaPlayerState.Playing;
		public bool IsMuted => _mp.SafeGetValue(mp => mp.IsMuted, false);
		public int? SubtitleTrackIndex => null;
		public int? AudioTrackIndex => null;
		public double Volume
		{
			get
			{
				try
				{
					return _mp.Volume;
				}
				catch
				{
					return 0;
				}
			}
			set
			{
				try
				{
					_mp.Volume = value;
				}
				catch { }
			}
		}


		public IObservable<TimeSpan> DurationChanged => _mp.SafeGetValue(mp => mp.PlaybackSession, null)?.Events().NaturalDurationChanged.Select(_ => _mp.SafeGetValue(mp => mp.NaturalDuration, TimeSpan.Zero)) ?? Observable.Empty<TimeSpan>();
		public IObservable<TimeSpan> PositionChanged => _mp.SafeGetValue(mp => mp.PlaybackSession, null)?.Events().PositionChanged.Select(_ => _mp.SafeGetValue(mp => mp.Position, TimeSpan.Zero)) ?? Observable.Empty<TimeSpan>();
		public IObservable<Unit> Playing => _mp.PlaybackSession.Events().PlaybackStateChanged.Where(_ => _mp.CurrentState is Windows.Media.Playback.MediaPlayerState.Playing).Select(_ => Unit.Default);
		public IObservable<Unit> Paused => _mp.PlaybackSession.Events().PlaybackStateChanged.Where(_ => _mp.CurrentState is Windows.Media.Playback.MediaPlayerState.Paused).Select(_ => Unit.Default);
		public IObservable<Unit> Ended => _mp.Events().MediaEnded.Select(_ => Unit.Default);
		public IObservable<Unit> Errored => _mp.Events().MediaFailed.Select(_ => Unit.Default);
		public IObservable<Unit> Stopped => _mp.PlaybackSession.Events().PlaybackStateChanged.Where(_ => _mp.CurrentState is Windows.Media.Playback.MediaPlayerState.Stopped).Select(_ => Unit.Default);
		public IObservable<Unit> MediaLoaded => _mp.Events().MediaOpened.Select(_ => Unit.Default);
		public IObservable<double> VolumeChanged => _mp.Events().VolumeChanged.Select(_ => _mp.Volume);
		public IObservable<string> SubtitleText { get; } = Observable.Empty<string>();

		public void Dispose() => _mp.Dispose();

		public IEnumerable<Core.Contracts.Services.AudioTrack> GetAudioTracks()
		{
			if (_mediaItem is null)
			{
				return [];
			}

			return _mediaItem.AudioTracks.Select(x => new Core.Contracts.Services.AudioTrack(_mediaItem.AudioTracks.IndexOf(x), x.Language, x.Label));
		}

		public void OpenAudioTrack(int index)
		{
			if (_mediaItem is null)
			{
				return;
			}

			_mediaItem.AudioTracks.SelectedIndex = index;
		}

		public void OpenExternalSubtitleTrack(string url)
		{
			if (_mediaItem is null)
			{
				return;
			}

			var track = TimedTextSource.CreateFromUri(new Uri(url));
			_mediaItem.Source.ExternalTimedTextSources.Add(track);
			track.Resolved += Track_Resolved;
		}

		public void OpenInternalSubtitleTrack(int trackIndex, int subtitleIndex)
		{
			if (_mediaItem is null or { TimedMetadataTracks: null })
			{
				return;
			}

			var timedMetadataTracks = _mediaItem.TimedMetadataTracks;
			if (subtitleIndex >= 0 && subtitleIndex < timedMetadataTracks.Count)
			{
				foreach (var track in timedMetadataTracks.Index())
				{
					_mediaItem.TimedMetadataTracks.SetPresentationMode((uint)track.Index, Windows.Media.Playback.TimedMetadataTrackPresentationMode.Disabled);
				}

				_mediaItem.TimedMetadataTracks.SetPresentationMode((uint)subtitleIndex, Windows.Media.Playback.TimedMetadataTrackPresentationMode.PlatformPresented);
			}
		}

		public void DisableSubtitles()
		{
			if (_mediaItem is null or { TimedMetadataTracks: null })
			{
				return;
			}

			var timedMetadataTracks = _mediaItem.TimedMetadataTracks;
			foreach (var track in timedMetadataTracks.Index())
			{
				_mediaItem.TimedMetadataTracks.SetPresentationMode((uint)track.Index, Windows.Media.Playback.TimedMetadataTrackPresentationMode.Disabled);
			}
		}

		public void Pause()
		{
			try
			{
				_mp.Pause();
			}
			catch { }
		}

		public bool Play()
		{
			try
			{
				_mp.Play();
			}
			catch { }
			return true;
		}

		public bool Play(Uri uri, int defaultAudioIndex = 0)
		{
			var source = MediaSource.CreateFromUri(uri);
			_mediaItem = new Windows.Media.Playback.MediaPlaybackItem(source);
			_mediaItem.AudioTracksChanged += (sender, args) => _audioTracksButton.DispatcherQueue.TryEnqueue(() => _audioTracksButton.Flyout = Converters.Converters.GetAudiosFlyout(this, defaultAudioIndex));
			_mp.Source = _mediaItem;
			_mp.Play();
			return true;
		}

		public void SeekTo(TimeSpan timeSpan)
		{
			try
			{
				_mp.Position = timeSpan;
			}
			catch { }
		}
		public void Stop() => Pause();

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
			args.Tracks[0].PlaybackItem.TimedMetadataTracks.SetPresentationMode((uint)index, Windows.Media.Playback.TimedMetadataTrackPresentationMode.PlatformPresented);
			sender.Resolved -= Track_Resolved;
		}
	}
}
