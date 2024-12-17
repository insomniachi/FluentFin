using FlyleafLib.MediaPlayer;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Xaml.Interactivity;

namespace FluentFin.Behaviors;

public partial class AudioButtonBehavior : Behavior<Button>
{
	public Player MediaPlayer
	{
		get { return (Player)GetValue(MediaPlayerProperty); }
		set { SetValue(MediaPlayerProperty, value); }
	}

	public static readonly DependencyProperty MediaPlayerProperty =
		DependencyProperty.Register("MediaPlayer", typeof(Player), typeof(AudioButtonBehavior), new PropertyMetadata(null, OnMediaPlayerChanged));

	private static void OnMediaPlayerChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (e.NewValue is not Player player)
		{
			return;
		}

		var behavior = (AudioButtonBehavior)d;

		if (e.OldValue is Player old)
		{
			old.Audio.Streams.CollectionChanged -= behavior.Streams_CollectionChanged;
		}

		player.Audio.Streams.CollectionChanged += behavior.Streams_CollectionChanged;

		if (behavior.AssociatedObject is not Button button)
		{
			return;
		}

		behavior.UpdateButtonVisibility();
	}

	private void Streams_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
	{
		try
		{
			if (MediaPlayer is not Player player)
			{
				return;
			}

			if (AssociatedObject is not Button button)
			{
				return;
			}

			button.DispatcherQueue.TryEnqueue(() =>
			{
				button.Flyout = Converters.Converters.GetAudiosFlyout (player, player.Audio.Streams);
			});
		}
		catch { }
	}

	protected override void OnAttached()
	{
		UpdateButtonVisibility();
	}

	private void UpdateButtonVisibility()
	{
		try
		{
			if (MediaPlayer is not Player player)
			{
				return;
			}

			if (AssociatedObject is not Button button)
			{
				return;
			}

			button.DispatcherQueue.TryEnqueue(() =>
			{
				button.Visibility = player.Audio.Streams.Count > 1 ? Visibility.Visible : Visibility.Collapsed;
			});
		}
		catch { }
	}
}
