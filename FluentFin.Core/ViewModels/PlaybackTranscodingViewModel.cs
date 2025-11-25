using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentFin.Contracts.ViewModels;
using FluentFin.Core.Contracts.Services;
using FluentFin.Core.Services;

namespace FluentFin.Core.ViewModels;

public partial class PlaybackTranscodingViewModel(IJellyfinClient jellyfinClient) : ObservableObject, INavigationAware
{
	private TranscodingSettings? _options;

	[ObservableProperty]
	public partial List<JellyfinConfigItemViewModel> Items { get; set; } = [];

	public Task OnNavigatedFrom() => Task.CompletedTask;

	public async Task OnNavigatedTo(object parameter)
	{
		_options = await jellyfinClient.GetTranscodeOptions();

		if (_options is null)
		{
			return;
		}

		Items =
		[
			new JellyfinConfigItemViewModel<string>(() => _options.QsvDevice ?? "", value => _options.QsvDevice = value)
			{
				DisplayName = "QSV Device",
				Description = "Specify the device for Intel QSV on a multi-GPU system. On Linux, this is the render node, e.g., /dev/dri/renderD128. " +
							  "On Windows, this is the device index starting from 0. Leave blank unless you know what you are doing."
			},
			new JellyfinConfigItemViewModel<bool>(() => _options.PreferSystemNativeHwDecoder, value => _options.PreferSystemNativeHwDecoder = value)
			{
				DisplayName = "Prefer OS native DXVA or VA-API hardware decoders",
			},
			new JellyfinGroupedConfigItemViewModel
			{
				DisplayName = "Hardware encoding options",
				Items =
				[
					new JellyfinConfigItemViewModel<bool>(() => _options.EnableHardwareEncoding, value => _options.EnableHardwareEncoding = value)
					{
						DisplayName = "Enable hardware encoding",
					},
					new JellyfinConfigItemViewModel<bool>(() => _options.EnableIntelLowPowerH264HwEncoder, value => _options.EnableIntelLowPowerH264HwEncoder = value)
					{
						DisplayName = "Enable Intel Low-Power H.264 hardware encoder",
					},
					new JellyfinConfigItemViewModel<bool>(() => _options.EnableIntelLowPowerHevcHwEncoder, value => _options.EnableIntelLowPowerHevcHwEncoder = value)
					{
						DisplayName = "Enable Intel Low-Power HEVC hardware encoder",
					},
				]
			},
			new JellyfinGroupedConfigItemViewModel
			{
				DisplayName = "Encoding format options",
				Description = "Select the video encoding that Jellyfin should transcode to. Jellyfin will use software encoding when hardware acceleration for the selected format is not available. H264 encoding will always be enabled.",
				Items =
				[
					new JellyfinConfigItemViewModel<bool>(() => _options.AllowHevcEncoding, value => _options.AllowHevcEncoding = value)
					{
						DisplayName = "Allow encoding in HEVC format",
					},
					new JellyfinConfigItemViewModel<bool>(() => _options.AllowAv1Encoding, value => _options.AllowAv1Encoding = value)
					{
						DisplayName = "Allow encoding in AV1 format",
					},
				]
			},
			new JellyfinConfigItemViewModel<bool>(() => _options.EnableVppTonemapping, value => _options.EnableVppTonemapping = value)
			{
				DisplayName = "Enable VPP Tone mapping",
				Description = "Full Intel driver based tone-mapping. Currently works only on certain hardware with HDR10 videos. This has a higher priority compared to another OpenCL implementation.",
			},
			new JellyfinConfigItemViewModel<double>(() => _options.VppTonemappingBrightness, value => _options.VppTonemappingBrightness = value)
			{
				DisplayName = "VPP Tone mapping brightness gain",
				Description = "Apply brightness gain in VPP tone mapping. The recommended and default values are 16 and 0."
			},
			new JellyfinConfigItemViewModel<double>(() => _options.VppTonemappingContrast, value => _options.VppTonemappingContrast = value)
			{
				DisplayName = "VPP Tone mapping contrast gain",
				Description = "Apply contrast gain in VPP tone mapping. The recommended and default values are 16 and 0."
			},
			new JellyfinConfigItemViewModel<bool>(() => _options.EnableTonemapping, value => _options.EnableTonemapping = value)
			{
				DisplayName = "Enable Tone mapping",
				Description = "Tone-mapping can transform the dynamic range of a video from HDR to SDR while maintaining image details and colors, which are very important information for representing the original scene. " +
							  "Currently works only with 10bit HDR10, HLG and DoVi videos. This requires the corresponding GPGPU runtime."
			},
			new JellyfinConfigItemViewModel<double>(() => _options.TonemappingDesat, value => _options.TonemappingDesat = value)
			{
				DisplayName = "Tone mapping desat",
				Description = "Apply desaturation for highlights that exceed this level of brightness. The higher the parameter, the more color information will be preserved. " +
							  "This setting helps prevent unnaturally blown-out colors for super-highlights, by (smoothly) turning into white instead. " +
							  "This makes images feel more natural, at the cost of reducing information about out-of-range colors. The recommended and default values are 0 and 0.5."
			},
			new JellyfinConfigItemViewModel<double>(() => _options.TonemappingPeak, value => _options.TonemappingPeak = value)
			{
				DisplayName = "Tone mapping peak",
				Description = "Override signal/nominal/reference peak with this value. Useful when the embedded peak information in display metadata is not reliable or when tone mapping from a lower range to a higher range. The recommended and default values are 100 and 0."
			},
			new JellyfinConfigItemViewModel<double>(() => _options.TonemappingParam, value => _options.TonemappingParam = value)
			{
				DisplayName = "Tone mapping param",
				Description = "Tune the tone mapping algorithm. The recommended and default values are NaN. Generally leave it blank."
			},
			new JellyfinConfigItemViewModel<bool>(() => _options.EnableFallbackFont, value => _options.EnableFallbackFont = value)
			{
				DisplayName = "Enable fallback fonts",
				Description = "Enable custom alternate fonts. This can avoid the problem of incorrect subtitle rendering."
			},
			new JellyfinConfigItemViewModel<bool>(() => _options.EnableAudioVbr, value => _options.EnableAudioVbr = value)
			{
				DisplayName = "Enable VBR audio encoding",
				Description = "Variable bitrate offers better quality to average bitrate ratio, but in some rare cases may cause buffering and compatibility issues."
			},
			new JellyfinConfigItemViewModel<double>(() => _options.DownMixAudioBoost, value => _options.DownMixAudioBoost = value)
			{
				DisplayName = "Audio boost when downmixing",
				Description = "Boost audio when downmixing. A value of one will preserve the original volume."
			},
			new JellyfinConfigItemViewModel<double>(() => _options.MaxMuxingQueueSize, value => _options.MaxMuxingQueueSize = (int)value)
			{
				DisplayName = "Maximum muxing queue size",
				Description = "Maximum number of packets that can be buffered while waiting for all streams to initialize. Try to increase it if you still meet Too many packets buffered for output stream\" error in FFmpeg logs. The recommended value is 2048."
			},
			new JellyfinGroupedConfigItemViewModel()
			{
				DisplayName = "Encoding CRF",
				Description = "The 'Constant Rate Factor' (CRF) is the default quality setting for the x264 and x265 software encoders. You can set the values between 0 and 51, where lower values would result in better quality (at the expense of higher file sizes). " +
							  "Sane values are between 18 and 28. The default for x264 is 23, and for x265 is 28, so you can use this as a starting point. Hardware encoders do not use these settings.",
				Items =
				[
					new JellyfinConfigItemViewModel<double>(() => _options.H265Crf, value => _options.H265Crf = (int)value)
					{
						DisplayName = "H.265 CRF",
					},
					new JellyfinConfigItemViewModel<double>(() => _options.H264Crf, value => _options.H264Crf = (int)value)
					{
						DisplayName = "H.264 CRF",
					}
				]
			},
			new JellyfinConfigItemViewModel<bool>(() => _options.DeinterlaceDoubleRate, value => _options.DeinterlaceDoubleRate = value)
			{
				DisplayName = "Double the frame rate when deinterlacing",
				Description = "This setting uses the field rate when deinterlacing, often referred to as bob deinterlacing, which doubles the frame rate of the video to provide full motion like what you would see when viewing interlaced video on a TV."
			},
			new JellyfinConfigItemViewModel<bool>(() => _options.EnableSubtitleExtraction, value => _options.EnableSubtitleExtraction = value)
			{
				DisplayName = "Allow subtitle extraction on the fly",
				Description = "Embedded subtitles can be extracted from videos and delivered to clients in plain text, in order to help prevent video transcoding. " +
							  "On some systems this can take a long time and cause video playback to stall during the extraction process. Disable this to have embedded subtitles burned in with video transcoding when they are not natively supported by the client device."
			},
			new JellyfinConfigItemViewModel<bool>(() => _options.EnableThrottling, value => _options.EnableThrottling = value)
			{
				DisplayName = "Throttle Transcodes",
				Description = "When a transcode or remux gets far enough ahead from the current playback position, pause the process so it will consume fewer resources. This is most useful when watching without seeking often. Turn this off if you experience playback issues."
			},
			new JellyfinConfigItemViewModel<bool>(() => _options.EnableSegmentDeletion, value => _options.EnableSegmentDeletion = value)
			{
				DisplayName = "Delete segments",
				Description = "Delete old segments after they have been downloaded by the client. This prevents having to store the entire transcoded file on disk. Turn this off if you experience playback issues."
			},
			new JellyfinConfigItemViewModel<double>(() => _options.ThrottleDelaySeconds, value => _options.ThrottleDelaySeconds = (int)value)
			{
				DisplayName = "Throttle after",
				Description = "Time in seconds after which the transcoder will be throttled. Must be large enough for the client to maintain a healthy buffer. Only works if throttling is enabled."
			},
			new JellyfinConfigItemViewModel<double>(() => _options.SegmentKeepSeconds, value => _options.SegmentKeepSeconds = (int)value)
			{
				DisplayName = "Time to keep segments",
				Description = "Time in seconds for which segments should be kept after they are downloaded by the client. Only works if segment deletion is enabled."
			},
		];

	}

	[RelayCommand]
	private async Task Save()
	{
		if (_options is null)
		{
			return;
		}

		foreach (var item in Items)
		{
			item.Save();
		}

		await jellyfinClient.SaveTranscodeOptions(_options);
	}

	[RelayCommand]
	private void Reset()
	{
		foreach (var item in Items)
		{
			item.Reset();
		}
	}
}
