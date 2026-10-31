using System.Web;
using FluentFin.Core.Contracts.Services;
using Flurl;
using Jellyfin.Sdk.Generated.Models;
using Microsoft.Extensions.Logging;

namespace FluentFin.Core.Services;



public partial class JellyfinClient
{

	public async Task<MediaResponse?> GetMediaUrl(BaseItemDto dto)
	{
		if (dto.Id is not { } id)
		{
			return null;
		}

		if (dto.Type is not (BaseItemDto_Type.Episode or BaseItemDto_Type.Movie))
		{
			return null;
		}

		if (dto.MediaType is not BaseItemDto_MediaType.Video)
		{
			return null;
		}

		var endPointInfo = await EndpointInfo();
		var bitRate = endPointInfo?.IsInNetwork == true ? 0 : await BitrateTest();

		var startTime = TimeProvider.System.GetTimestamp();
		var playbackInfoDto = new PlaybackInfoDto
		{
			UserId = UserId,
			AutoOpenLiveStream = true,
			DeviceProfile = deviceProfileFactory.GetDeviceProfile(),
			StartTimeTicks = startTime,
		};

		if (bitRate != 0)
		{
			playbackInfoDto.MaxStreamingBitrate = bitRate;
		}

		var playbackInfo = await _jellyfinApiClient.Items[id].PlaybackInfo.PostAsync(playbackInfoDto);

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

		if (!string.IsNullOrEmpty(mediaSource.TranscodingUrl) && mediaSource.SupportsTranscoding == true)
		{
			var uri = new Uri(HttpUtility.UrlDecode(BaseUrl.AppendPathSegment(mediaSource.TranscodingUrl)));
			return new(AddApiKey(uri), PlaybackProgressInfo_PlayMethod.Transcode, sessionId, mediaSource.Id ?? "", mediaSource);
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
		else
		{
			return null;
		}
	}

	public Uri GetImage(BaseItemDto item, ImageInfo info)
	{
		return BaseUrl.AppendPathSegment($"/Items/{item.Id}/Images/{info.ImageType}").SetQueryParam("tag", info.ImageTag).ToUri();
	}

	public async Task Playing(PlaybackStartInfo info)
	{
		try
		{
			await _jellyfinApiClient.Sessions.Playing.PostAsync(info);
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
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
		}
	}

	public async Task<bool> Stop(PlaybackStopInfo info)
	{
		try
		{
			await _jellyfinApiClient.Sessions.Playing.Stopped.PostAsync(info);
			return true;
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
			return false;
		}
	}

	public async Task<bool> StopEncoding(string playSessionId)
	{
		try
		{
			// not part of the generated SDK
			var request = new Microsoft.Kiota.Abstractions.RequestInformation
			{
				HttpMethod = Microsoft.Kiota.Abstractions.Method.DELETE,
				UrlTemplate = "{+baseurl}/Videos/ActiveEncodings{?deviceId,playSessionId}",
				PathParameters = new Dictionary<string, object> { ["baseurl"] = BaseUrl },
				QueryParameters = { ["deviceId"] = _deviceId, ["playSessionId"] = playSessionId },
			};
			await _requestAdapter.SendNoContentAsync(request);
			return true;
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
			return false;
		}
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

	public async Task<List<SessionInfoDto>> GetControllableSessions()
	{
		try
		{
			return await _jellyfinApiClient.Sessions.GetAsync(x => x.QueryParameters.ControllableByUserId = UserId) ?? [];
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
			return [];
		}
	}

	public async Task PlayOnSession(string sessionId, params IEnumerable<Guid?> items)
	{
		await _jellyfinApiClient.Sessions[sessionId].Playing.PostAsync(x =>
		{
			x.QueryParameters.ItemIds = [.. items];
			x.QueryParameters.PlayCommand = Jellyfin.Sdk.Generated.Sessions.Item.Playing.PlayCommand.PlayNow;
		});
	}

	public async Task TogglePlayPause(string sessionId)
	{
		try
		{
			await _jellyfinApiClient.Sessions[sessionId].Playing["PlayPause"].PostAsync();
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
			return;
		}
	}

	public async Task Stop(string sessionId)
	{
		try
		{
			await _jellyfinApiClient.Sessions[sessionId].Playing["Stop"].PostAsync();
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
			return;
		}
	}

	public Uri GetTrickplayImage(BaseItemDto dto, int index, int resolution)
	{
		return new Uri(HttpUtility.HtmlDecode(BaseUrl.AppendPathSegment($"/Videos/{dto?.Id}/Trickplay/{resolution}/{index}.jpg").AppendQueryParam("ApiKey", _token).ToString()));
	}

}
