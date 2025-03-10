using CommunityToolkit.WinUI;
using FluentFin.ViewModels;
using FlyleafLib.MediaFramework.MediaStream;
using FlyleafLib.MediaPlayer;
using LibVLCSharp.Shared;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ReactiveMarbles.ObservableEvents;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Windows.Input;


namespace FluentFin.Controls;

#nullable disable
public sealed partial class TransportControls : UserControl
{
	private readonly Subject<Microsoft.UI.Xaml.Input.PointerRoutedEventArgs> _onPointerMoved = new();

	[GeneratedDependencyProperty]
	public partial bool IsSkipButtonVisible { get; set; }

	[GeneratedDependencyProperty]
	public partial PlaylistViewModel Playlist { get; set; }


	[GeneratedDependencyProperty]
	public partial ICommand SkipCommand { get; set; }

	[GeneratedDependencyProperty]
	public partial TrickplayViewModel Trickplay { get; set; }

	public MediaPlayer Player
	{
		get
		{
			try
			{
				return (MediaPlayer)GetValue(PlayerProperty);
			}
			catch
			{
				return null;
			}
		}
		set { SetValue(PlayerProperty, value); }
	}

	public static readonly DependencyProperty PlayerProperty =
		DependencyProperty.Register("Player", typeof(MediaPlayer), typeof(TransportControls), new PropertyMetadata(null));

    public IObservable<Unit> OnDynamicSkip { get; }

	public TransportControls()
	{
		InitializeComponent();

		OnDynamicSkip = DynamicSkipIntroButton.Events().Click.Select(_ => Unit.Default);

		TimeSlider
			.Events()
			.ValueChanged
			.Where(x => Math.Abs(x.NewValue - Player.Time) > 1000)
			.Subscribe(x => Player.SeekTo(TimeSpan.FromSeconds(x.NewValue)));

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
				Trickplay.Position = TimeSpan.FromSeconds((point.Position.X / TimeSlider.ActualWidth) * TimeSlider.Maximum);

				var minMargin = Math.Max(teachingTipMargin + offset, globalPoint.Position.X + offset - halfTrickplayWidth);
				var margin = Math.Min(minMargin, ActualWidth + offset - trickplayWidth - 10);
				TrickplayTip.PlacementMargin = new Thickness(margin, 0, 0, Bar.ActualHeight);
			});
	}

	public string TimeRemaining(long currentTime, long duration)
	{
		var remaining = duration - currentTime;
		return TimeSpan.FromMilliseconds(remaining).ToString("hh\\:mm\\:ss");
	}

	private void SkipBackwardButton_Click(object sender, RoutedEventArgs e)
	{
		var ts = TimeSpan.FromMilliseconds(Player.Time) - TimeSpan.FromSeconds(10);
		Player.SeekTo(ts);
	}

	private void SkipForwardButton_Click(object sender, RoutedEventArgs e)
	{
		var ts = TimeSpan.FromMilliseconds(Player.Time) + TimeSpan.FromSeconds(30);
		Player.SeekTo(ts);
	}


	public void UpdateSubtitleFlyout(ObservableCollection<SubtitlesStream> streams)
	{
		//var flyout = CCSelectionButton.Flyout as MenuFlyout;
		//flyout.Items.Clear();
		//for (int i = 0; i < streams.Count; i++)
		//{
		//	var item = new RadioMenuFlyoutItem
		//	{
		//		Text = streams[i].Title,
		//		IsChecked = i == 0,
		//	};
		//	item.Click += Item_Click;

		//	flyout.Items.Add(item);
		//}
	}

	private void Item_Click(object sender, RoutedEventArgs e)
	{
		//var title = ((RadioMenuFlyoutItem)sender).Text;
		//var stream = Player.Subtitles.Streams.FirstOrDefault(x => x.Title == title);

		//if (stream is null)
		//{
		//	return;
		//}

		//var flyout = CCSelectionButton.Flyout as MenuFlyout;
		//Player.OpenAsync(stream);
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
}

#nullable restore