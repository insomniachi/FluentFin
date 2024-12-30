using FluentFin.ViewModels;
using FlyleafLib.MediaFramework.MediaStream;
using FlyleafLib.MediaPlayer;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ReactiveMarbles.ObservableEvents;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Input;


namespace FluentFin.Controls;

#nullable disable
public sealed partial class TransportControls : UserControl
{
	public bool IsSkipButtonVisible
	{
		get { return (bool)GetValue(IsSkipButtonVisibleProperty); }
		set { SetValue(IsSkipButtonVisibleProperty, value); }
	}

	public string SelectedResolution
	{
		get { return (string)GetValue(SelectedResolutionProperty); }
		set { SetValue(SelectedResolutionProperty, value); }
	}

	public Player Player
	{
		get 
		{
			try
			{
				return (Player)GetValue(PlayerProperty);
			}
			catch
			{
				return null;
			}
		}
		set { SetValue(PlayerProperty, value); }
	}

	public IEnumerable<string> Resolutions
	{
		get { return (IEnumerable<string>)GetValue(ResolutionsProperty); }
		set { SetValue(ResolutionsProperty, value); }
	}


	public PlaylistViewModel Playlist
	{
		get { return (PlaylistViewModel)GetValue(PlaylistProperty); }
		set { SetValue(PlaylistProperty, value); }
	}


	public ICommand SkipCommand
	{
		get { return (ICommand)GetValue(SkipCommandProperty); }
		set { SetValue(SkipCommandProperty, value); }
	}

	public static readonly DependencyProperty SkipCommandProperty =
		DependencyProperty.Register("SkipCommand", typeof(ICommand), typeof(TransportControls), new PropertyMetadata(null));

	public static readonly DependencyProperty PlaylistProperty =
		DependencyProperty.Register("Playlist", typeof(PlaylistViewModel), typeof(TransportControls), new PropertyMetadata(null));


	public static readonly DependencyProperty ResolutionsProperty =
		DependencyProperty.Register("Resolutions", typeof(IEnumerable<string>), typeof(TransportControls), new PropertyMetadata(null));

	public static readonly DependencyProperty SelectedResolutionProperty =
		DependencyProperty.Register("SelectedResolution", typeof(string), typeof(TransportControls), new PropertyMetadata(""));


	public static readonly DependencyProperty IsSkipButtonVisibleProperty =
		DependencyProperty.Register("IsSkipButtonVisible", typeof(bool), typeof(TransportControls), new PropertyMetadata(false));

	public static readonly DependencyProperty PlayerProperty =
		DependencyProperty.Register("Player", typeof(Player), typeof(TransportControls), new PropertyMetadata(null));

	public IObservable<Unit> OnDynamicSkip { get; }

	public TransportControls()
	{
		InitializeComponent();

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