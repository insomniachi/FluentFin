using FluentFin.Core.ViewModels;
using Jellyfin.Sdk.Generated.Models;
using Microsoft.UI.Xaml.Controls;


namespace FluentFin.Views;

public sealed partial class MoviePage : Page
{
	public MovieViewModel ViewModel { get; } = App.GetService<MovieViewModel>();

	public MoviePage()
	{
		InitializeComponent();
	}

	public static string? GetVideoTitle(BaseItemDto? dto) => dto?.MediaStreams?.FirstOrDefault(x => x.Type == MediaStream_Type.Video)?.DisplayTitle;

	public static string? GetSelectedAudio(BaseItemDto? dto) => dto?.MediaStreams?.FirstOrDefault(x => x.Type == MediaStream_Type.Audio)?.DisplayTitle;

	public static IEnumerable<string?> GetAudioStreams(BaseItemDto? dto) => dto?.MediaStreams?.Where(x => x.Type == MediaStream_Type.Audio)?.Select(x => x.DisplayTitle) ?? [];

	public static string? GetSelectedSubtitle(BaseItemDto? dto) => dto?.MediaStreams?.FirstOrDefault(x => x.Type == MediaStream_Type.Subtitle)?.DisplayTitle;
	public static IEnumerable<string?> GetSubtitleStreams(BaseItemDto? dto) => dto?.MediaStreams?.Where(x => x.Type == MediaStream_Type.Subtitle)?.Select(x => x.DisplayTitle) ?? [];

	public static IEnumerable<BaseItemPerson> GetDirectors(List<BaseItemPerson>? people) => people?.Where(x => x.Type == BaseItemPerson_Type.Director) ?? [];
	public static IEnumerable<BaseItemPerson> GetWriters(List<BaseItemPerson>? people) => people?.Where(x => x.Type == BaseItemPerson_Type.Writer) ?? [];
}
