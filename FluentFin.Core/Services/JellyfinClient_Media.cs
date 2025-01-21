using FluentFin.Core.Contracts.Services;
using Flurl;
using Jellyfin.Sdk.Generated.Models;
using Microsoft.Extensions.Logging;
using System.Web;

namespace FluentFin.Core.Services;



public partial class JellyfinClient
{
	public async Task<MediaResponse?> GetMediaUrl(BaseItemDto dto)
	{
		if (dto.Id is not { } id)
		{
			return null;
		}

		var startTime = TimeProvider.System.GetTimestamp();

		if (dto.Type is BaseItemDto_Type.Movie or BaseItemDto_Type.Episode)
		{
			var endPointInfo = await EndpointInfo();

			var bitRate = endPointInfo?.IsInNetwork == true ? int.MaxValue : await BitrateTest();

			var playbackInfo = await _jellyfinApiClient.Items[id].PlaybackInfo.PostAsync(new PlaybackInfoDto
			{
				UserId = UserId,
				AutoOpenLiveStream = true,
				DeviceProfile = DeviceProfiles.Flyleaf,
				MaxStreamingBitrate = bitRate,
				StartTimeTicks = startTime,
			});

			if (playbackInfo is null or { PlaySessionId: null } or { MediaSources: null })
			{
				return null;
			}

			var sessionId = playbackInfo.PlaySessionId;
			var mediaSource = playbackInfo.MediaSources.FirstOrDefault(x => x.Id == id.ToString("N"));

			if (mediaSource is null)
			{
				return null;
			}

			if (dto.MediaType == BaseItemDto_MediaType.Video)
			{
				if (!string.IsNullOrEmpty(mediaSource.TranscodingUrl) && mediaSource.SupportsTranscoding == true)
				{
					return new(BaseUrl.AppendPathSegment(mediaSource.TranscodingUrl).ToUri(), PlaybackProgressInfo_PlayMethod.Transcode, sessionId, mediaSource.Id ?? "", mediaSource);
				}
				else if (mediaSource?.SupportsDirectPlay == true)
				{
					var info = _jellyfinApiClient.Videos[id].Stream.ToGetRequestInformation(x =>
					{
						var query = x.QueryParameters;
						query.Container = mediaSource.Container;
						query.PlaySessionId = sessionId;
						query.Static = true;
						query.Tag = mediaSource.ETag;
						query.StartTimeTicks = startTime;
					});

					return new(AddApiKey(info.URI), PlaybackProgressInfo_PlayMethod.DirectPlay, sessionId, mediaSource.Id ?? "", mediaSource);
				}
			}
		}

		return null;
	}

	public Uri GetImage(BaseItemDto item, ImageInfo info)
	{
		return BaseUrl.AppendPathSegment($"/Items/{item.Id}/Images/{info.ImageType}").SetQueryParam("tag", info.ImageTag).ToUri();
	}

	public async Task Playing(BaseItemDto dto)
	{
		try
		{
			await _jellyfinApiClient.Sessions.Playing.PostAsync(new PlaybackStartInfo
			{
				ItemId = dto?.Id,
			});
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
		}
	}

	public async Task Progress(PlaybackProgressInfo info)
	{
		try
		{
			await _jellyfinApiClient.Sessions.Playing.Progress.PostAsync(info);
			_currentItem = info;
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
		}
	}

	public async Task Stop(PlaybackStopInfo info)
	{
		try
		{
			await _jellyfinApiClient.Sessions.Playing.Stopped.PostAsync(info);
			_currentItem = null;
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
		}
	}

	public async Task Stop()
	{
		if (_currentItem is null)
		{
			return;
		}

		var info = new PlaybackStopInfo
		{
			ItemId = _currentItem.ItemId,
			MediaSourceId = _currentItem.MediaSourceId,
			PositionTicks = _currentItem.PositionTicks,
			SessionId = _currentItem.SessionId,
		};

		await Stop(info);
	}

	public Uri? GetStreamUrl(BaseItemDto dto)
	{
		if (dto.Id is not { } id)
		{
			return null;
		}

		var info = _jellyfinApiClient.Items[id].Download.ToGetRequestInformation();
		return AddApiKey(info.URI);
	}

	public async Task<MediaSegmentDtoQueryResult?> GetMediaSegments(BaseItemDto dto, MediaSegmentType[]? types = null)
	{
		if (dto.Id is not { } id)
		{
			return null;
		}

		try
		{
			return await _jellyfinApiClient.MediaSegments[id].GetAsync(x => x.QueryParameters.IncludeSegmentTypes = types);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
			return null;
		}
	}

	public Uri GetTrickplayImage(BaseItemDto dto, int index, int resolution)
	{
		return new Uri(HttpUtility.HtmlDecode(BaseUrl.AppendPathSegment($"/Videos/{dto?.Id}/Trickplay/{resolution}/{index}.jpg").AppendQueryParam("api_key", _token).ToString()));
	}
}
