using FlyleafLib.MediaFramework.MediaStream;
using FlyleafLib.MediaPlayer;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ReactiveMarbles.ObservableEvents;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;


namespace FluentFin.Controls;

#nullable disable
public sealed partial class TransportControls : UserControl
{
	public bool IsNextTrackButtonVisible
	{
		get { return (bool)GetValue(IsNextTrackButtonVisibleProperty); }
		set { SetValue(IsNextTrackButtonVisibleProperty, value); }
	}

	public bool IsPreviousTrackButtonVisible
	{
		get { return (bool)GetValue(IsPreviousTrackButtonVisibleProperty); }
		set { SetValue(IsPreviousTrackButtonVisibleProperty, value); }
	}

	public bool IsSkipButtonVisible
	{
		get { return (bool)GetValue(IsSkipButtonVisibleProperty); }
		set { SetValue(IsSkipButtonVisibleProperty, value); }
	}

	public bool IsAddCCButtonVisibile
	{
		get { return (bool)GetValue(IsAddCCButtonVisibileProperty); }
		set { SetValue(IsAddCCButtonVisibileProperty, value); }
	}

	public bool IsCCSelectionVisible
	{
		get { return (bool)GetValue(IsCCSelectionVisibleProperty); }
		set { SetValue(IsCCSelectionVisibleProperty, value); }
	}

	public string SelectedResolution
	{
		get { return (string)GetValue(SelectedResolutionProperty); }
		set { SetValue(SelectedResolutionProperty, value); }
	}

	public Player Player
	{
		get { return (Player)GetValue(PlayerProperty); }
		set { SetValue(PlayerProperty, value); }
	}

	public IEnumerable<string> Resolutions
	{
		get { return (IEnumerable<string>)GetValue(ResolutionsProperty); }
		set { SetValue(ResolutionsProperty, value); }
	}

	public static readonly DependencyProperty ResolutionsProperty =
		DependencyProperty.Register("Resolutions", typeof(IEnumerable<string>), typeof(TransportControls), new PropertyMetadata(null));

	public static readonly DependencyProperty SelectedResolutionProperty =
		DependencyProperty.Register("SelectedResolution", typeof(string), typeof(TransportControls), new PropertyMetadata(""));

	public static readonly DependencyProperty IsCCSelectionVisibleProperty =
		DependencyProperty.Register("IsCCSelectionVisible", typeof(bool), typeof(TransportControls), new PropertyMetadata(false));

	public static readonly DependencyProperty IsAddCCButtonVisibileProperty =
		DependencyProperty.Register("IsAddCCButtonVisibile", typeof(bool), typeof(TransportControls), new PropertyMetadata(false));

	public static readonly DependencyProperty IsSkipButtonVisibleProperty =
		DependencyProperty.Register("IsSkipButtonVisible", typeof(bool), typeof(TransportControls), new PropertyMetadata(false));

	public static readonly DependencyProperty IsPreviousTrackButtonVisibleProperty =
		DependencyProperty.Register("IsPreviousTrackButtonVisible", typeof(bool), typeof(TransportControls), new PropertyMetadata(false));

	public static readonly DependencyProperty IsNextTrackButtonVisibleProperty =
		DependencyProperty.Register("IsNextTrackButtonVisible", typeof(bool), typeof(TransportControls), new PropertyMetadata(false));

	public static readonly DependencyProperty PlayerProperty =
	DependencyProperty.Register("Player", typeof(Player), typeof(TransportControls), new PropertyMetadata(null));


	public IObservable<Unit> OnNextTrack { get; }
	public IObservable<Unit> OnPrevTrack { get; }
	public IObservable<Unit> OnDynamicSkip { get; }

	public TransportControls()
	{
		InitializeComponent();


		OnNextTrack = NextTrackButton.Events().Click.Select(_ => Unit.Default);
		OnPrevTrack = PreviousTrackButton.Events().Click.Select(_ => Unit.Default);
		OnDynamicSkip = DynamicSkipIntroButton.Events().Click.Select(_ => Unit.Default);


		TimeSlider
			.Events()
			.ValueChanged
			.Where(x => Math.Abs(x.NewValue - Converters.Converters.TiksToSeconds(Player.CurTime)) > 1)
			.Subscribe(x => Player.SeekAccurate((int)TimeSpan.FromSeconds(x.NewValue).TotalMilliseconds));
	}

	public string TimeRemaining(long currentTime, long duration)
	{
		var remaining = duration - currentTime;
		return new TimeSpan(remaining).ToString("hh\\:mm\\:ss");
	}

	private void SkipBackwardButton_Click(object sender, RoutedEventArgs e)
	{
		var ts = new TimeSpan(Player.CurTime) - TimeSpan.FromSeconds(10);
		Player.SeekAccurate((int)ts.TotalMilliseconds);
	}

	private void SkipForwardButton_Click(object sender, RoutedEventArgs e)
	{
		var ts = new TimeSpan(Player.CurTime) + TimeSpan.FromSeconds(30);
		Player.SeekAccurate((int)ts.TotalMilliseconds);
	}


	public void UpdateSubtitleFlyout(ObservableCollection<SubtitlesStream> streams)
	{
		var flyout = CCSelectionButton.Flyout as MenuFlyout;
		flyout.Items.Clear();
		for (int i = 0; i < streams.Count; i++)
		{
			var item = new RadioMenuFlyoutItem
			{
				Text = streams[i].Title,
				IsChecked = i == 0,
			};
			item.Click += Item_Click;

			flyout.Items.Add(item);
		}
	}

	private void Item_Click(object sender, RoutedEventArgs e)
	{
		var title = ((RadioMenuFlyoutItem)sender).Text;
		var stream = Player.Subtitles.Streams.FirstOrDefault(x => x.Title == title);

		if (stream is null)
		{
			return;
		}

		var flyout = CCSelectionButton.Flyout as MenuFlyout;
		Player.OpenAsync(stream);
	}
}

#nullable restore