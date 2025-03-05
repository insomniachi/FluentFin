using FluentFin.Core.Contracts.Services;
using Jellyfin.Sdk.Generated.Models;

namespace FluentFin.Core.ViewModels;

public partial class PlaybackTrickplayViewModel(IJellyfinClient jellyfinClient) : ServerConfigurationViewModel(jellyfinClient)
{

    protected override List<JellyfinConfigItemViewModel> CreateItems(ServerConfiguration config)
    {
		if(config.TrickplayOptions is not { } trickplay)
		{
			return [];
		}

		return
		[
			new JellyfinConfigItemViewModel<bool>(() => trickplay.EnableHwAcceleration ?? false, value => trickplay.EnableHwAcceleration = value)
			{
				DisplayName = "Enable hardware decoding",
            },
			new JellyfinConfigItemViewModel<bool>(() => trickplay.EnableHwEncoding ?? false, value => trickplay.EnableHwEncoding = value)
			{
				DisplayName = "Enable hardware accelerated MJPEG encoding",
				Description = "Currently only available on QSV, VA-API, VideoToolbox and RKMPP, this option has no effect on other hardware acceleration methods."
            },
            new JellyfinConfigItemViewModel<bool>(() => trickplay.EnableKeyFrameOnlyExtraction ?? false, value => trickplay.EnableKeyFrameOnlyExtraction = value)
			{
				DisplayName = "Only generate images from key frames",
				Description = "Extract key frames only for significantly faster processing with less accurate timing. If the configured hardware decoder does not support this mode, will use the software decoder instead."
            },
			new JellyfinSelectableConfigItemViewModel(() => trickplay.ScanBehavior,
													  value => trickplay.ScanBehavior = value as TrickplayOptions_ScanBehavior?,
													  [.. Enum.GetValues<TrickplayOptions_ScanBehavior>()])
			{
                DisplayName = "Scan Behavior",
                Description = "The default behavior is non blocking, which will add media to the library before trickplay generation is done. " +
							  "Blocking will ensure trickplay files are generated before media is added to the library, but will make scans significantly longer."
            },
            new JellyfinSelectableConfigItemViewModel(() => trickplay.ProcessPriority,
                                                      value => trickplay.ProcessPriority = value as TrickplayOptions_ProcessPriority?,
                                                      [.. Enum.GetValues<TrickplayOptions_ProcessPriority>()])
			{
				DisplayName = "Process Priority",
				Description = "Setting this lower or higher will determine how the CPU prioritizes the ffmpeg trickplay generation process in relation to other processes. " +
							  "If you notice slowdown while generating trickplay images but don't want to fully stop their generation, try lowering this as well as the thread count."
            },
            new JellyfinConfigItemViewModel<double>(() => trickplay.Interval ?? 0, value => trickplay.Interval = (int)value)
			{
				DisplayName = "Image Interval",
				Description = "Interval of time (ms) between each new trickplay image."
            },
            new JellyfinConfigItemViewModel<string>(() => string.Join(",", trickplay.WidthResolutions ?? []), value => trickplay.WidthResolutions = [.. value.Split(",").Select(x => int.TryParse(x, out var num) ? num : (int?)null)])
			{
				DisplayName = "Width Resolutions",
				Description = "Comma separated list of the widths (px) that trickplay images will be generated at. All images should generate proportionally to the source, so a width of 320 on a 16:9 video ends up around 320x180."
            },
            new JellyfinConfigItemViewModel<double>(() => trickplay.TileWidth ?? 0, value => trickplay.TileWidth = (int)value)
			{
				DisplayName = "Tile Width",
				Description = "Maximum number of images per tile in the X direction."
            },
            new JellyfinConfigItemViewModel<double>(() => trickplay.TileHeight ?? 0, value => trickplay.TileHeight = (int)value)
			{
                DisplayName = "Tile Height",
                Description = "Maximum number of images per tile in the Y direction."
            },
            new JellyfinConfigItemViewModel<double>(() => trickplay.JpegQuality ?? 0, value => trickplay.JpegQuality = (int)value)
			{
				DisplayName = "JPEG Quality",
				Description = "The JPEG compression quality for trickplay images."
            },
            new JellyfinConfigItemViewModel<double>(() => trickplay.Qscale ?? 0, value => trickplay.Qscale = (int)value)
			{
				DisplayName = "Qscale",
				Description = "The quality scale of images output by ffmpeg, with 2 being the highest quality and 31 being the lowest."
            },
			new JellyfinConfigItemViewModel<double>(() => trickplay.ProcessThreads ?? 0, value => trickplay.ProcessThreads = (int)value)
			{
                DisplayName = "FFmpeg Threads",
                Description = "The number of threads to pass to the '-threads' argument of ffmpeg."
            }
        ];
    }
}
