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

        if(_options is null)
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
            new JellyfinConfigItemViewModel<double>(() => _options.VppTonemappingBrightness, value => _options.VppTonemappingBrightness = (int)value)
            {
                DisplayName = "VPP Tone mapping brightness gain",
                Description = "Apply brightness gain in VPP tone mapping. The recommended and default values are 16 and 0."
            },
            new JellyfinConfigItemViewModel<double>(() => _options.VppTonemappingContrast, value => _options.VppTonemappingContrast = (int)value)
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
            new JellyfinConfigItemViewModel<double>(() => _options.TonemappingDesat, value => _options.TonemappingDesat = (int)value)
            {
                DisplayName = "Tone mapping desat",
                Description = "Apply desaturation for highlights that exceed this level of brightness. The higher the parameter, the more color information will be preserved. " +
                              "This setting helps prevent unnaturally blown-out colors for super-highlights, by (smoothly) turning into white instead. " +
                              "This makes images feel more natural, at the cost of reducing information about out-of-range colors. The recommended and default values are 0 and 0.5."
            },
            new JellyfinConfigItemViewModel<double>(() => _options.TonemappingPeak, value => _options.TonemappingPeak = (int)value)
            {
                DisplayName = "Tone mapping peak",
                Description = "Override signal/nominal/reference peak with this value. Useful when the embedded peak information in display metadata is not reliable or when tone mapping from a lower range to a higher range. The recommended and default values are 100 and 0."
            }
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
