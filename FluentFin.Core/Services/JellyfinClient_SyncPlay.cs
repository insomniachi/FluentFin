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
    }
}
