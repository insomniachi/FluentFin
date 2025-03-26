using Jellyfin.Sdk.Generated.Models;
using Microsoft.Extensions.Logging;

namespace FluentFin.Core.Services
{
    public partial class JellyfinClient
    {
        public async Task<List<GroupInfoDto>> GetSyncPlayGroups()
        {
            try
            {
                return await _jellyfinApiClient.SyncPlay.List.GetAsync() ?? [];
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled Exception");
                return [];
            }
        }

        public async Task CreateSyncPlayGroup()
        {
            try
            {
                await _jellyfinApiClient.SyncPlay.New.PostAsync(new NewGroupRequestDto
                {
                    GroupName = $"{SessionInfo.CurrentUser.Name}'s Group"
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled Exception");
                return;
            }
        }

        public async Task JoinSyncPlayGroup(Guid groupId)
        {
            try
            {
                await _jellyfinApiClient.SyncPlay.Join.PostAsync(new JoinGroupRequestDto
                {
                    GroupId = groupId
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled Exception");
                return;
            }
        }

        public async Task LeaveSyncPlayGroup()
        {
            try
            {
                await _jellyfinApiClient.SyncPlay.Leave.PostAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled Exception");
                return;
            }
        }

        public async Task SignalReadyForSyncPlay(ReadyRequestDto request)
        {
            try
            {
                await _jellyfinApiClient.SyncPlay.Ready.PostAsync(request);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled Exception");
                return;
            }
        }

        public async Task SignalPauseForSyncPlay()
        {
            try
            {
                await _jellyfinApiClient.SyncPlay.Pause.PostAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled Exception");
                return;
            }
        }

        public async Task SignalUnpauseForSyncPlay()
        {
            try
            {
                await _jellyfinApiClient.SyncPlay.Unpause.PostAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled Exception");
                return;
            }
        }

        public async Task SignalSeekForSyncPlay(TimeSpan position)
        {
            try
            {
                await _jellyfinApiClient.SyncPlay.Seek.PostAsync(new SeekRequestDto
                {
                    PositionTicks = position.Ticks,
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled Exception");
                return;
            }
        }

        public async Task<DateTimeOffset> SyncTime()
        {
            var localTime = TimeProvider.System.GetUtcNow();
            try
            {
                var response = await _jellyfinApiClient.GetUtcTime.GetAsync();
                if(response is null || response.RequestReceptionTime is null)
                {
                    return localTime;
                }

                var pingStart = TimeProvider.System.GetTimestamp();
                var  result = await _jellyfinApiClient.System.Ping.GetAsync();
                var ping = TimeProvider.System.GetElapsedTime(pingStart);
                var diff = (response.RequestReceptionTime.Value - (ping/2)) - localTime;

                return localTime + diff;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled Exception");
                return localTime;
            }
        }
    }
}
