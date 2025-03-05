using FluentFin.Core.Contracts.Services;
using Jellyfin.Sdk.Generated.Models;

namespace FluentFin.Core.ViewModels;

public partial class PlaybackResumeViewModel(IJellyfinClient jellyfinClient) : ServerConfigurationViewModel(jellyfinClient)
{
    protected override List<JellyfinConfigItemViewModel> CreateItems(ServerConfiguration config)
    {
        return 
        [
            new JellyfinConfigItemViewModel<double>(() => config.MinResumePct ?? 0, value => config.MinResumePct = (int?)value)
            {
                DisplayName = "Minimum resume percentage",
                Description = "Titles are assumed unplayed if stopped before this time.",
            },
			new JellyfinConfigItemViewModel<double>(() => config.MaxResumePct ?? 0, value => config.MaxResumePct = (int?)value)
            {
                DisplayName = "Maximum resume percentage",
                Description = "Titles are assumed fully played if stopped after this time.",
            },
			new JellyfinConfigItemViewModel<double>(() => config.MinResumeDurationSeconds ?? 0, value => config.MinResumeDurationSeconds = (int?)value)
            {
                DisplayName = "Minimum resume duration",
                Description = "The shortest video length in seconds that will save playback location and let you resume.",
            },
            new JellyfinConfigItemViewModel <double>(() => config   .RemoteClientBitrateLimit ?? 0, value => config.RemoteClientBitrateLimit = (int?)value)
            {
                DisplayName = "Internet streaming bitrate limit (Mbps)",
                Description = "An optional per-stream bitrate limit for all out of network devices. This is useful to prevent devices from requesting a higher bitrate than your internet connection can handle. " +
                              "This may result in increased CPU load on your server in order to transcode videos on the fly to a lower bitrate.",
            }
        ];
    }
}
