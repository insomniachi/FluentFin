using System.Reactive.Linq;
using CommunityToolkit.WinUI;
using FluentFin.Plugins.Jellyseer.Models;
using Microsoft.UI.Xaml.Controls;
using ReactiveUI;
using System;
using System.Threading.Tasks;
using System.Reactive.Threading.Tasks;
using Microsoft.UI.Xaml.Media.Imaging;
using Flurl;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ListView;

namespace FluentFin.Plugins.Jellyseer.Views;

public sealed partial class JellyseerRequest : UserControl
{
	public JellyseerRequest()
	{
		InitializeComponent();

		this.WhenAnyValue(x => x.Request)
			.WhereNotNull()
			.Where(_ => Client is not null)
			.SelectMany(x => FetchItemDeteails(x).ToObservable())
			.Subscribe();

		this.WhenAnyValue(x => x.Client)
			.WhereNotNull()
			.Where(_ => Request is not null)
			.SelectMany(x => FetchItemDeteails(Request!).ToObservable())
			.Subscribe();
	}

	[GeneratedDependencyProperty]
	public partial MediaRequest? Request { get; set; }

	[GeneratedDependencyProperty]
	public partial IJellyseerClient? Client { get; set; }

	[GeneratedDependencyProperty]
	public partial AdditionalData? Data { get; private set; }

	private async Task FetchItemDeteails(MediaRequest request)
	{
		if (!request.AdditionalData.TryGetValue("type", out var type))
		{
			return;
		}

		if (type is "movie")
		{
			var details = await Client!.GetMovieDetails(request);
			DispatcherQueue.TryEnqueue(() =>
			{
				Data = new AdditionalData(GetProxyImage(details?.PosterPath), GetProxyImage(details?.BackdropPath), details?.OriginalTitle!, request?.RequestedBy?.Email!);
			});
		}
		else if (type is "tv")
		{
			var details = await Client!.GetSeriesDetails(request);
			DispatcherQueue.TryEnqueue(() =>
			{
				Data = new AdditionalData(GetProxyImage(details?.PosterPath), GetProxyImage(details?.BackdropPath), details?.OriginalName!, request?.RequestedBy?.Email!);
			});
		}
	}

	private BitmapImage GetProxyImage(string? path, int width = 1920, int quality = 75)
	{
		return new BitmapImage(Client!.BaseUrl!.AppendPathSegment("/_next/image")
		.SetQueryParams(new
		{
			url = "https://image.tmdb.org/t/p/original/".AppendPathSegment(path).ToUri(),
			w = width,
			q = quality
		})
		.ToUri());
	}
}


public record AdditionalData(BitmapImage Poster, BitmapImage Backdrop, string Title, string RequestedBy);

