using CommunityToolkit.Mvvm.Input;
using FluentFin.Core.Contracts.Services;
using FluentFin.ViewModels;
using Flurl;
using Jellyfin.Sdk.Generated.Models;
using LibVLCSharp.Shared;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Collections.ObjectModel;
using System.Web;
using Windows.Foundation;

namespace FluentFin.Converters;

public static class Converters
{
	public static IEnumerable<int> GetDummyItemSource(int count) => Enumerable.Range(0, count);
	public static string? GetVideoTitle(BaseItemDto? dto) => dto?.MediaStreams?.FirstOrDefault(x => x.Type == MediaStream_Type.Video)?.DisplayTitle;
	public static string? GetSelectedAudio(BaseItemDto? dto) => dto?.MediaStreams?.FirstOrDefault(x => x.Type == MediaStream_Type.Audio)?.DisplayTitle;
	public static IEnumerable<string?> GetAudioStreams(BaseItemDto? dto) => dto?.MediaStreams?.Where(x => x.Type == MediaStream_Type.Audio)?.Select(x => x.DisplayTitle) ?? [];
	public static string? GetSelectedSubtitle(BaseItemDto? dto) => dto?.MediaStreams?.FirstOrDefault(x => x.Type == MediaStream_Type.Subtitle)?.DisplayTitle;
	public static IEnumerable<string?> GetSubtitleStreams(BaseItemDto? dto) => dto?.MediaStreams?.Where(x => x.Type == MediaStream_Type.Subtitle)?.Select(x => x.DisplayTitle) ?? [];
	public static IEnumerable<BaseItemPerson> GetDirectors(List<BaseItemPerson>? people) => people?.Where(x => x.Type == BaseItemPerson_Type.Director) ?? [];
	public static IEnumerable<BaseItemPerson> GetWriters(List<BaseItemPerson>? people) => people?.Where(x => x.Type == BaseItemPerson_Type.Writer) ?? [];
	public static double TicksToSeconds(long value) => value / 10000000.0;
	public static long SecondsToTicks(double value) => (long)(value * 10000000.0);
	public static string TicksToTime(long value) => new TimeSpan(value).ToString("hh\\:mm\\:ss");
	public static string MsToTime(long ms) => TimeSpan.FromMilliseconds(ms).ToString("hh\\:mm\\:ss");

    public static string DateTimeOffsetToString(DateTimeOffset? offset)
	{
		if(offset is null)
		{
			return "";
		}

		return offset.Value.Date.ToLocalTime().ToShortDateString();
	}

	public static string TicksToTime2(long? value)
	{
		if(value is null)
		{
			return "";
		}

		var ts = new TimeSpan(value.Value);
		if(ts.Hours > 0)
		{
			return string.Format($"{ts.Hours}h {ts.Minutes}m");
		}
		else
		{
			return string.Format($"{ts.Minutes}m");
		}
	}
	public static string TicksToSecondsString(long value) => TimeSpanToString(new TimeSpan(value));
	public static Visibility VisibleIfMoreThanOne(ObservableCollection<PlaylistItem> items) => VisibleIfMoreThanOne<PlaylistItem>(items);
	public static Visibility VisibleIfMoreThanOne<T>(IList<T> values) => values.Count > 1 ? Visibility.Visible : Visibility.Collapsed;

	public static string TimeSpanToString(TimeSpan ts)
	{
		return ts.Hours > 0 ? ts.ToString("hh\\:mm\\:ss") : ts.ToString("mm\\:ss");
	}
	public static double ToDouble(long? value) => value ?? 0;
	public static Guid ToGuid(string value)
	{
		return Guid.TryParse(value, out var guid) ? guid : Guid.Empty;
	}
	public static Rect ToRect(RectModel? clip) => clip is null ? new() : new(clip.X, clip.Y, clip.Width, clip.Height);
	public static ImageSource? GetImage(Uri? uri)
	{
		if (uri is null)
		{
			return null;
		}

		return new BitmapImage(uri);
	}

	public static FlyoutBase? GetAudiosFlyout(MediaPlayer player, MediaResponse response)
	{
		if(player.Media is not { } media)
		{
			return null;
		}
		var defaultIndex = response?.MediaSourceInfo.DefaultAudioStreamIndex ?? 0;

		var tracks = media.Tracks.ToList();
        var audios = tracks.Where(x => x.TrackType == TrackType.Audio).ToList();

        const string groupName = "Audios";
		var command = new RelayCommand<MediaTrack>(track => player.SetAudioTrack(track.Id));

		var flyout = new MenuBarItemFlyout();

		foreach (var item in audios)
		{
			var flyoutItem = new RadioMenuFlyoutItem
			{
				Text = $"{item.Language} ({item.Description})",
				GroupName = groupName,
				IsChecked = item.Id == defaultIndex,
				Command = command,
				CommandParameter = item
			};

			flyout.Items.Add(flyoutItem);
		}

		return flyout;
	}

	public static FlyoutBase? GetSubtitlesFlyout(MediaPlayer mp, MediaResponse response)
	{
        var subtitles = response?.MediaSourceInfo.MediaStreams?.Where(x => x.Type == MediaStream_Type.Subtitle).ToList() ?? [];
        var defaultSubtitleIndex = response?.MediaSourceInfo.DefaultSubtitleStreamIndex;

        if (subtitles.Count == 0)
        {
            return null;
        }

        var command = new RelayCommand<MediaStream>(stream =>
        {
            if (stream is null)
            {
                return;
            }

            if (stream.IsExternal == true)
            {
                var url = HttpUtility.UrlDecode(App.GetService<IJellyfinClient>().BaseUrl.AppendPathSegment(stream.DeliveryUrl).ToString());
				mp.AddSlave(MediaSlaveType.Subtitle, url, true);
            }
            else
            {
                var internalSubtitleInfo = subtitles.Where(x => x.IsExternal is false or null).ToList();
                var index = internalSubtitleInfo.IndexOf(stream);
                mp.SetSpu(index);
            }
        });
        var disableSubtitles = new RelayCommand(() =>
        {
			mp.SetSpu(-1);
        });

        const string groupName = "Subtitles";
        var flyout = new MenuBarItemFlyout();
        flyout.Items.Add(new RadioMenuFlyoutItem
        {
            Text = "None",
            GroupName = groupName,
            Command = disableSubtitles,
            IsChecked = response?.MediaSourceInfo.DefaultSubtitleStreamIndex is null
        });

        foreach (var item in subtitles)
        {

            var flyoutItem = new RadioMenuFlyoutItem
            {
                Text = $"{item.DisplayTitle}",
                GroupName = groupName,
                IsChecked = item.Index == defaultSubtitleIndex,
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
