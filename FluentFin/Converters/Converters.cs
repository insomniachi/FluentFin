using CommunityToolkit.Mvvm.Input;
using FlyleafLib.MediaFramework.MediaStream;
using FlyleafLib.MediaPlayer;
using Jellyfin.Sdk.Generated.Models;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Media;

namespace FluentFin.Converters;

public static class Converters
{
	public static string? GetVideoTitle(BaseItemDto? dto) => dto?.MediaStreams?.FirstOrDefault(x => x.Type == MediaStream_Type.Video)?.DisplayTitle;
	public static string? GetSelectedAudio(BaseItemDto? dto) => dto?.MediaStreams?.FirstOrDefault(x => x.Type == MediaStream_Type.Audio)?.DisplayTitle;
	public static IEnumerable<string?> GetAudioStreams(BaseItemDto? dto) => dto?.MediaStreams?.Where(x => x.Type == MediaStream_Type.Audio)?.Select(x => x.DisplayTitle) ?? [];
	public static string? GetSelectedSubtitle(BaseItemDto? dto) => dto?.MediaStreams?.FirstOrDefault(x => x.Type == MediaStream_Type.Subtitle)?.DisplayTitle;
	public static IEnumerable<string?> GetSubtitleStreams(BaseItemDto? dto) => dto?.MediaStreams?.Where(x => x.Type == MediaStream_Type.Subtitle)?.Select(x => x.DisplayTitle) ?? [];
	public static IEnumerable<BaseItemPerson> GetDirectors(List<BaseItemPerson>? people) => people?.Where(x => x.Type == BaseItemPerson_Type.Director) ?? [];
	public static IEnumerable<BaseItemPerson> GetWriters(List<BaseItemPerson>? people) => people?.Where(x => x.Type == BaseItemPerson_Type.Writer) ?? [];
	public static double TiksToSeconds(long value) => value / 10000000.0;
	public static long SecondsToTicks(double value) => (long)(value * 10000000.0);
	public static string TicksToTime(long value) => new TimeSpan(value).ToString("hh\\:mm\\:ss");
	public static double ToDouble(long? value) => value ?? 0;
	public static Guid ToGuid(string value) => Guid.Parse(value);
	public static ImageSource? GetImage(Uri? uri)
	{
		if(uri is null)
		{
			return null;
		}

		return new BitmapImage(uri);
	}

	public static FlyoutBase? GetSubtitlesFlyout(Player player, IList<SubtitlesStream> subtitles)
	{
		if(subtitles is null || player is null)
		{
			return null;
		}

		if(subtitles.Count < 1)
		{
			return null;
		}

		const string groupName = "Subtitles";
		var command = new RelayCommand<SubtitlesStream>(stream =>
		{
			player.Config.Subtitles.Enabled = true;
			player.Open(stream);
		});
		var disableSubtitles = new RelayCommand(() =>
		{
			player.Config.Subtitles.Enabled = false;
		});

		var flyout = new MenuBarItemFlyout();
		flyout.Items.Add(new RadioMenuFlyoutItem
		{
			Text = "None",
			GroupName = groupName,
			Command = disableSubtitles,
			IsChecked = !subtitles.Any(x => x.Enabled)
		});


		foreach (var item in subtitles)
		{
			var flyoutItem = new RadioMenuFlyoutItem
			{
				Text = $"{item.Language}",
				GroupName = groupName,
				IsChecked = item.Enabled,
				Command = command,
				CommandParameter = item
			};

			flyout.Items.Add(flyoutItem);
		}

		return flyout;
	}

	public static FlyoutBase? GetAudiosFlyout(Player player, IList<AudioStream> audios)
	{
		if (audios is null || player is null)
		{
			return null;
		}

		if (audios.Count < 1)
		{
			return null;
		}

		const string groupName = "Audios";
		var command = new RelayCommand<AudioStream>(stream =>
		{
			player.Open(stream);
		});

		var flyout = new MenuBarItemFlyout();
		
		foreach (var item in audios)
		{
			var flyoutItem = new RadioMenuFlyoutItem
			{
				Text = $"{item.Language} ({item.Codec})",
				GroupName = groupName,
				IsChecked = item.Enabled,
				Command = command,
				CommandParameter = item
			};

			flyout.Items.Add(flyoutItem);
		}

		return flyout;
	}


	public static string AccessScheduleToString(AccessSchedule schedule)
	{
		if (schedule is null)
		{
			return string.Empty;
		}

		var start = DateTime.Today.Add(TimeSpan.FromHours(schedule.StartHour ?? 0));
		var end = DateTime.Today.Add(TimeSpan.FromHours(schedule.EndHour ?? 0));

		return $"{start:hh:mm tt} - {end:hh:mm tt}";
	}
}
