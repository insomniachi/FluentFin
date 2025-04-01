using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows.Input;
using CommunityToolkit.WinUI;
using FluentFin.Core;
using FluentFin.Core.Contracts.Services;
using FluentFin.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ReactiveMarbles.ObservableEvents;
using ReactiveUI;


namespace FluentFin.Controls;

#nullable disable
public sealed partial class TransportControls : UserControl
{
	private readonly Subject<Microsoft.UI.Xaml.Input.PointerRoutedEventArgs> _onPointerMoved = new();
	private readonly SymbolIcon _playSymbol = new(Symbol.Play);
	private readonly SymbolIcon _pauseSymbol = new(Symbol.Pause);

	[GeneratedDependencyProperty]
	public partial bool IsSkipButtonVisible { get; set; }

	[GeneratedDependencyProperty]
	public partial PlaylistViewModel Playlist { get; set; }


	[GeneratedDependencyProperty]
	public partial ICommand SkipCommand { get; set; }

	[GeneratedDependencyProperty]
	public partial TrickplayViewModel Trickplay { get; set; }

	[GeneratedDependencyProperty]
	public partial IJellyfinClient JellyfinClient { get; set; }

	public IMediaPlayerController Player
	{
		get
		{
			try
			{
				return (IMediaPlayerController)GetValue(PlayerProperty);
			}
			catch
			{
				return null;
			}
		}
		set { SetValue(PlayerProperty, value); }
	}

	public static readonly DependencyProperty PlayerProperty =
		DependencyProperty.Register("Player", typeof(IMediaPlayerController), typeof(TransportControls), new PropertyMetadata(null, OnPlayerChanged));

	private static void OnPlayerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		var tc = (TransportControls)d;
		if (e.NewValue is not IMediaPlayerController controller)
		{
			return;
		}

		TimeSpan duration = TimeSpan.Zero;
		controller.DurationChanged.ObserveOn(RxApp.MainThreadScheduler).Subscribe(e =>
		{
			tc.TimeSlider.Maximum = e.TotalMilliseconds;
			duration = e;
		});
		controller.PositionChanged.ObserveOn(RxApp.MainThreadScheduler).Subscribe(e =>
		{
			try
			{
				tc.TimeSlider.Value = e.TotalMilliseconds;
				tc.TxtCurrentTime.Text = Converters.Converters.TimeSpanToString(e);
				tc.TxtRemainingTime.Text = TimeRemaining(e, duration);
			}
			catch { }
		});
		controller.Playing.ObserveOn(RxApp.MainThreadScheduler).Subscribe(_ => tc.PlayPauseButton.Content = tc._pauseSymbol);
		controller.Paused.ObserveOn(RxApp.MainThreadScheduler).Subscribe(_ => tc.PlayPauseButton.Content = tc._playSymbol);
		controller.VolumeChanged
			.Where(e => e >= 0)
			.Throttle(TimeSpan.FromSeconds(200))
			.ObserveOn(RxApp.MainThreadScheduler)
			.Subscribe(e => tc.VolumeSlider.Value = Math.Floor(e));
		controller.SubtitleText.ObserveOn(RxApp.MainThreadScheduler).Subscribe(text => tc.Subtitles.Text = text);
	}

	public IObservable<Unit> OnDynamicSkip { get; }

	public TransportControls()
	{
		InitializeComponent();

		OnDynamicSkip = DynamicSkipIntroButton.Events().Click.Select(_ => Unit.Default);


		TimeSlider
			.Events()
			.ValueChanged
			.Where(x => Math.Abs(x.NewValue - Player.Position.TotalMilliseconds) > 5000)
			.Subscribe(x =>
			{
				try
				{
					var currentState = Player.State;
					if (currentState is MediaPlayerState.Playing)
					{
						Player.Pause();
					}

					Player.SeekTo(TimeSpan.FromMilliseconds(x.NewValue));

					if (currentState is MediaPlayerState.Playing)
					{
						Player.Play();
					}
				}
				catch { }
			});

		_onPointerMoved
			.ObserveOn(RxApp.MainThreadScheduler)
			.Subscribe(e =>
			{
				const int teachingTipMargin = 12;
				TrickplayTip.IsOpen = true;

				var navView = this.FindAscendantOrSelf<NavigationView>();
				var offset = navView?.IsPaneOpen == true ? navView.OpenPaneLength : 0;
				var trickplayWidth = TrickplayScrollViewer.Width + 2 * teachingTipMargin;
				var halfTrickplayWidth = trickplayWidth / 2;

				var point = e.GetCurrentPoint(TimeSlider);
				var globalPoint = e.GetCurrentPoint(this);
				Trickplay.Position = TimeSpan.FromMilliseconds((point.Position.X / TimeSlider.ActualWidth) * TimeSlider.Maximum);

				var minMargin = Math.Max(teachingTipMargin + offset, globalPoint.Position.X + offset - halfTrickplayWidth);
				var margin = Math.Min(minMargin, ActualWidth + offset - trickplayWidth - 10);
				TrickplayTip.PlacementMargin = new Thickness(margin, 0, 0, Bar.ActualHeight);
			});

		VolumeSlider.Events()
			.ValueChanged
			.Where(_ => Player is not null)
			.Subscribe(x => Player.Volume = (int)x.NewValue);
	}

	private static string TimeRemaining(TimeSpan currentTime, TimeSpan duration)
	{
		return (duration - currentTime).ToString("hh\\:mm\\:ss");
	}

	private async void SkipBackwardButton_Click(object sender, RoutedEventArgs e)
	{
		var ts = Player.Position - TimeSpan.FromSeconds(10);
		Player.SeekTo(ts);

		if (JellyfinClient is null)
		{
			return;
		}

		await JellyfinClient.SignalSeekForSyncPlay(ts);
	}

	private async void SkipForwardButton_Click(object sender, RoutedEventArgs e)
	{
		var ts = Player.Position + TimeSpan.FromSeconds(30);
		Player.SeekTo(ts);

		if (JellyfinClient is null)
		{
			return;
		}

		await JellyfinClient.SignalSeekForSyncPlay(ts);
	}

	private void TimeSlider_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
	{
		if (Trickplay is null)
		{
			return;
		}

		if (Trickplay.Item?.Trickplay?.AdditionalData?.Count is not > 0)
		{
			return;
		}

		_onPointerMoved.OnNext(e);
	}

	private void TimeSlider_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
	{
		TrickplayTip.IsOpen = false;
	}

	private void Grid_PointerMoved(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
	{
		TrickplayTip.IsOpen = false;
	}

	private async void PlayPauseButton_Click(object sender, RoutedEventArgs e)
	{
		Player.TogglePlayPlause();

		if (JellyfinClient is null)
		{
			return;
		}


		if (Player.State == MediaPlayerState.Playing)
		{
			await JellyfinClient.SignalUnpauseForSyncPlay();
		}
		else
		{
			await JellyfinClient.SignalPauseForSyncPlay();
		}
	}

	private async void CastButton_Click(object sender, RoutedEventArgs e)
	{
		if (JellyfinClient is null)
		{
			return;
		}

		var sessions = await JellyfinClient.GetControllableSessions();

		if (sessions.FirstOrDefault(x => x.Id == SessionInfo.SessionId) is { } session && session.NowPlayingItem is { } dto)
		{
			Player.Stop();
			App.Dialogs.PlayOnSessionCommand.Execute(dto);
		}
	}
}

#nullable restore