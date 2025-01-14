using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentFin.Contracts.ViewModels;
using FluentFin.Core.Contracts.Services;
using Jellyfin.Sdk.Generated.Models;

namespace FluentFin.Core.ViewModels;

public partial class PlaybackTrickplayViewModel(IJellyfinClient jellyfinClient) : ObservableObject, INavigationAware
{
	private ServerConfiguration? _serverConfiguration;

	[ObservableProperty]
	public partial bool EnableHardwareDecoding { get; set; }

	[ObservableProperty]
	public partial bool EnableHardwareAcceleratedMjpegEncoding { get; set; }

	[ObservableProperty]
	public partial bool OnlyGenerateImagesFromKeyFrames { get; set; }

	[ObservableProperty]
	public partial TrickplayOptions_ScanBehavior? ScanBehavior { get; set; }

	[ObservableProperty]
	public partial TrickplayOptions_ProcessPriority? ProcessPriority { get; set; }

	[ObservableProperty]
	public partial double ImageInterval { get; set; }

	[ObservableProperty]
	public partial string WidthResolutions { get; set; }

	[ObservableProperty]
	public partial double TileWidth { get; set; }

	[ObservableProperty]
	public partial double TileHeight { get; set; }

	[ObservableProperty]
	public partial double JpegQuality { get; set; }

	[ObservableProperty]
	public partial double Qscale { get; set; }

	[ObservableProperty]
	public partial double FfmpegThreads { get; set; }

	public Task OnNavigatedFrom() => Task.CompletedTask;

	public async Task OnNavigatedTo(object parameter)
	{
		_serverConfiguration = await jellyfinClient.GetConfiguration();

		if (_serverConfiguration?.TrickplayOptions is not { } trickplay)
		{
			return;
		}


		EnableHardwareDecoding = trickplay.EnableHwAcceleration ?? false;
		EnableHardwareAcceleratedMjpegEncoding = trickplay.EnableHwEncoding ?? false;
		OnlyGenerateImagesFromKeyFrames = trickplay.EnableKeyFrameOnlyExtraction ?? false;
		ScanBehavior = trickplay.ScanBehavior;
		ProcessPriority = trickplay.ProcessPriority;
		ImageInterval = trickplay.Interval ?? 0;
		WidthResolutions = string.Join(",", trickplay.WidthResolutions ?? []);
		TileWidth = trickplay.TileWidth ?? 0;
		TileHeight = trickplay.TileHeight ?? 0;
		JpegQuality = trickplay.JpegQuality ?? 0;
		Qscale = trickplay.Qscale ?? 0;
		FfmpegThreads = trickplay.ProcessThreads ?? 0;
	}

	[RelayCommand]
	private async Task Save()
	{
		if (_serverConfiguration?.TrickplayOptions is not { } trickplay)
		{
			return;
		}

		trickplay.EnableHwAcceleration = EnableHardwareDecoding;
		trickplay.EnableHwEncoding = EnableHardwareAcceleratedMjpegEncoding;
		trickplay.EnableKeyFrameOnlyExtraction = OnlyGenerateImagesFromKeyFrames;
		trickplay.ScanBehavior = ScanBehavior;
		trickplay.ProcessPriority = ProcessPriority;
		trickplay.Interval = (int?)ImageInterval;
		trickplay.WidthResolutions = WidthResolutions.Split(",").Select(x =>
		{
			return int.TryParse(x, out var num) ? num : (int?)null;
		}).Where(x => x is not null).ToList();
		trickplay.TileWidth = (int?)TileWidth;
		trickplay.TileHeight = (int?)TileHeight;
		trickplay.JpegQuality = (int?)JpegQuality;
		trickplay.Qscale = (int?)Qscale;
		trickplay.ProcessThreads = (int?)FfmpegThreads;

		await jellyfinClient.SaveConfiguration(_serverConfiguration);
	}


	[RelayCommand]
	private async Task Reset() => await OnNavigatedTo(new());
}
