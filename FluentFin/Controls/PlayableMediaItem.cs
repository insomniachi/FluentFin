using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;


namespace FluentFin.Controls;

public sealed partial class PlayableMediaItem : Control
{
	public string Subtitle
	{
		get { return (string)GetValue(SubtitleProperty); }
		set { SetValue(SubtitleProperty, value); }
	}

	public int UnplayedCount
	{
		get { return (int)GetValue(UnplayedCountProperty); }
		set { SetValue(UnplayedCountProperty, value); }
	}

	public string Title
	{
		get { return (string)GetValue(TitleProperty); }
		set { SetValue(TitleProperty, value); }
	}

	public ImageSource ImageSource
	{
		get { return (ImageSource)GetValue(ImageSourceProperty); }
		set { SetValue(ImageSourceProperty, value); }
	}

	public double Progress
	{
		get { return (double)GetValue(ProgressProperty); }
		set { SetValue(ProgressProperty, value); }
	}

	public bool IsWatched
	{
		get { return (bool)GetValue(IsWatchedProperty); }
		set { SetValue(IsWatchedProperty, value); }
	}

	public bool IsFavourite
	{
		get { return (bool)GetValue(IsFavouriteProperty); }
		set { SetValue(IsFavouriteProperty, value); }
	}

	public static readonly DependencyProperty IsFavouriteProperty =
		DependencyProperty.Register("IsFavourite", typeof(bool), typeof(PlayableMediaItem), new PropertyMetadata(false));


	public static readonly DependencyProperty IsWatchedProperty =
		DependencyProperty.Register("IsWatched", typeof(bool), typeof(PlayableMediaItem), new PropertyMetadata(false));


	public static readonly DependencyProperty ProgressProperty =
		DependencyProperty.Register(nameof(Progress), typeof(double), typeof(PlayableMediaItem), new PropertyMetadata(0));


	public static readonly DependencyProperty ImageSourceProperty =
		DependencyProperty.Register(nameof(ImageSource), typeof(ImageSource), typeof(PlayableMediaItem), new PropertyMetadata(null));


	public static readonly DependencyProperty TitleProperty =
		DependencyProperty.Register(nameof(Title), typeof(string), typeof(PlayableMediaItem), new PropertyMetadata(""));


	public static readonly DependencyProperty UnplayedCountProperty =
		DependencyProperty.Register(nameof(UnplayedCount), typeof(int), typeof(PlayableMediaItem), new PropertyMetadata(0));

	public static readonly DependencyProperty SubtitleProperty =
		DependencyProperty.Register("Subtitle", typeof(string), typeof(PlayableMediaItem), new PropertyMetadata(""));


	public PlayableMediaItem()
	{
		DefaultStyleKey = typeof(PlayableMediaItem);
	}
}
