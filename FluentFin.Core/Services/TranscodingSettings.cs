namespace FluentFin.Core.Services;

public class TranscodingSettings
{
	public int EncodingThreadCount { get; set; }
	public string? TranscodingTempPath { get; set; }
	public string? FallbackFontPath { get; set; }
	public bool EnableFallbackFont { get; set; }
	public bool EnableAudioVbr { get; set; }
	public double DownMixAudioBoost { get; set; }
	public string? DownMixStereoAlgorithm { get; set; }
	public int MaxMuxingQueueSize { get; set; }
	public bool EnableThrottling { get; set; }
	public int ThrottleDelaySeconds { get; set; }
	public bool EnableSegmentDeletion { get; set; }
	public int SegmentKeepSeconds { get; set; }
	public string? HardwareAccelerationType { get; set; }
	public string? EncoderAppPathDisplay { get; set; }
	public string? VaapiDevice { get; set; }
	public string? QsvDevice { get; set; }
	public bool EnableTonemapping { get; set; }
	public bool EnableVppTonemapping { get; set; }
	public bool EnableVideoToolboxTonemapping { get; set; }
	public string? TonemappingAlgorithm { get; set; }
	public string? TonemappingMode { get; set; }
	public string? TonemappingRange { get; set; }
	public double TonemappingDesat { get; set; }
	public double TonemappingPeak { get; set; }
	public double TonemappingParam { get; set; }
	public double VppTonemappingBrightness { get; set; }
	public double VppTonemappingContrast { get; set; }
	public int H264Crf { get; set; }
	public int H265Crf { get; set; }
	public string? EncoderPreset { get; set; }
	public bool DeinterlaceDoubleRate { get; set; }
	public string? DeinterlaceMethod { get; set; }
	public bool EnableDecodingColorDepth10Hevc { get; set; }
	public bool EnableDecodingColorDepth10Vp9 { get; set; }
	public bool EnableDecodingColorDepth10HevcRext { get; set; }
	public bool EnableDecodingColorDepth12HevcRext { get; set; }
	public bool EnableEnhancedNvdecDecoder { get; set; }
	public bool PreferSystemNativeHwDecoder { get; set; }
	public bool EnableIntelLowPowerH264HwEncoder { get; set; }
	public bool EnableIntelLowPowerHevcHwEncoder { get; set; }
	public bool EnableHardwareEncoding { get; set; }
	public bool AllowHevcEncoding { get; set; }
	public bool AllowAv1Encoding { get; set; }
	public bool EnableSubtitleExtraction { get; set; }
	public List<string> HardwareDecodingCodecs { get; set; } = [];
	public List<string> AllowOnDemandMetadataBasedKeyframeExtractionForExtensions { get; set; } = [];
}
