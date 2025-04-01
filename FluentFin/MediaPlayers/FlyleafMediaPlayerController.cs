using System.Reactive;
using System.Reactive.Linq;
using System.Reflection;
using System.Web;
using DynamicData.Binding;
using FluentFin.Core.Contracts.Services;
using FlyleafLib.Controls.WinUI;
using FlyleafLib.MediaPlayer;
using Microsoft.UI.Xaml.Controls;
using ReactiveUI;

namespace FluentFin.MediaPlayers;

public sealed partial class FlyleafMediaPlayerController : IMediaPlayerController
{
	private readonly Player _mp = new();
	private readonly Button _audiosFlyoutButton;
	private int _defaultAudioIndex;

	public FlyleafMediaPlayerController(FlyleafHost host, Button audioTracksButton)
	{
		_audiosFlyoutButton = audioTracksButton;
		_mp.Config.Player.KeyBindings.RemoveAll();
		host.Player = _mp;

		_mp.Audio.Streams
		   .ToObservableChangeSet()
		   .Throttle(TimeSpan.FromSeconds(1))
		   .ObserveOn(RxApp.MainThreadScheduler)
		   .Subscribe(_ =>
		   {
			   _audiosFlyoutButton.Flyout = Converters.Converters.GetAudiosFlyout(this, _defaultAudioIndex);
		   });
	}

	public MediaPlayerState State => ConvertState(_mp.Status);

	public TimeSpan Position => new(_mp.CurTime);
	public bool IsPlaying => _mp.IsPlaying;
	public bool IsMuted => _mp.Audio.Mute;
	public int? SubtitleTrackIndex => _mp.Subtitles.Streams.FirstOrDefault(x => x.Enabled)?.StreamIndex - 1;
	public int? AudioTrackIndex => _mp.Audio.Streams.FirstOrDefault(x => x.Enabled)?.StreamIndex - 1;

	public double Volume
	{
		get => _mp.Audio.Volume;
		set => _mp.Audio.Volume = (int)value;
	}

	public IObservable<TimeSpan> DurationChanged => _mp.WhenAnyValue(x => x.Duration).Select(x => new TimeSpan(x));
	public IObservable<TimeSpan> PositionChanged => _mp.WhenAnyValue(x => x.CurTime).Select(x => new TimeSpan(x));
	public IObservable<Unit> Playing => _mp.WhenAnyValue(x => x.Status).Where(x => x is Status.Playing).Select(_ => Unit.Default);
	public IObservable<Unit> Paused => _mp.WhenAnyValue(x => x.Status).Where(x => x is Status.Paused).Select(_ => Unit.Default);
	public IObservable<Unit> Ended => _mp.WhenAnyValue(x => x.Status).Where(x => x is Status.Ended).Select(_ => Unit.Default);
	public IObservable<Unit> Errored => _mp.WhenAnyValue(x => x.Status).Where(x => x is Status.Failed).Select(_ => Unit.Default);
	public IObservable<Unit> Stopped => _mp.WhenAnyValue(x => x.Status).Where(x => x is Status.Stopped).Select(_ => Unit.Default);
	public IObservable<double> VolumeChanged => _mp.WhenAnyValue(x => x.Audio.Volume).Select(x => x * 1d);
	public IObservable<string> SubtitleText => _mp.WhenAnyValue(x => x.Subtitles.SubsText);
	public IObservable<Unit> MediaLoaded { get; } = Observable.Empty<Unit>();

	public void Dispose() => _mp.Dispose();
	public IEnumerable<AudioTrack> GetAudioTracks() => _mp.Audio.Streams.Select(x => new AudioTrack(x.StreamIndex, x.Language.TopEnglishName, x.Title));
	public void OpenAudioTrack(int index)
	{
		if (_mp.Audio.Streams.FirstOrDefault(x => x.StreamIndex == index) is { } stream)
		{
			_mp.Open(stream);
		}
	}
	public void OpenExternalSubtitleTrack(string url)
	{
		_mp.Config.Subtitles.Enabled = true;
		MethodInfo? dynMethod = _mp.GetType().GetMethod("OpenSubtitles", BindingFlags.NonPublic | BindingFlags.Instance);
		dynMethod?.Invoke(_mp, [url]);
	}
	public void OpenInternalSubtitleTrack(int trackIndex, int subtitleIndex)
	{
		_mp.Config.Subtitles.Enabled = true;
		_mp.Open(_mp.Subtitles.Streams[subtitleIndex]);
	}

	public void DisableSubtitles() => _mp.Config.Subtitles.Enabled = false;

	public void Pause() => _mp.Pause();

	public bool Play()
	{
		_mp.Play();
		return true;
	}

	public bool Play(Uri uri, int defaultAudioIndex = 0)
	{
		_defaultAudioIndex = defaultAudioIndex;
		var args = _mp.Open(HttpUtility.UrlDecode(uri.ToString()));
		return args.Success;
	}

	public void SeekTo(TimeSpan timeSpan) => _mp.SeekAccurate((int)timeSpan.TotalMilliseconds);
	public void Stop() => _mp.Stop();

	private static MediaPlayerState ConvertState(Status status)
	{
		return status switch
		{
			Status.Opening => MediaPlayerState.Opening,
			Status.Playing => MediaPlayerState.Playing,
			Status.Paused => MediaPlayerState.Paused,
			Status.Stopped => MediaPlayerState.Stopped,
			Status.Ended => MediaPlayerState.Ended,
			Status.Failed => MediaPlayerState.Error,
			_ => throw new ArgumentOutOfRangeException(nameof(status), status, null)
		};
	}
}
