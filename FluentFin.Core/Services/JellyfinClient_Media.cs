using Flurl;
using Jellyfin.Sdk.Generated.Models;
using Microsoft.Extensions.Logging;
using System.Web;

namespace FluentFin.Core.Services;

public partial class JellyfinClient
{
	public async Task<Uri?> GetMediaUrl(BaseItemDto dto)
	{
		if (dto.Id is not { } id)
		{
			return null;
		}

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
				StartTimeTicks = dto.UserData?.PlaybackPositionTicks
			});

			if (playbackInfo is null or { PlaySessionId: null } or { MediaSources: null })
			{
				return null;
			}

			var sessionId = playbackInfo.PlaySessionId;
			var mediaSource = playbackInfo.MediaSources.FirstOrDefault(x => x.Id == id.ToString("N"));

			if (dto.MediaType == BaseItemDto_MediaType.Video)
			{
				if (!string.IsNullOrEmpty(mediaSource?.TranscodingUrl) && mediaSource?.SupportsTranscoding == true)
				{
					return BaseUrl.AppendPathSegment(mediaSource.TranscodingUrl).ToUri();
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
						query.StartTimeTicks = dto.UserData?.PlaybackPositionTicks;
					});

					return AddApiKey(info.URI);
				}
			}
		}

		return null;
	}

	public Uri? GetImage(BaseItemDto dto, ImageType type, double? height = null)
	{
		if (dto.Id is not { } id)
		{
			return null;
		}

		if(dto.ImageTags is null)
		{
			return null;
		}

		object? requestTag = null;
		var hasRequestTag = dto.ImageTags.AdditionalData.TryGetValue($"{type}", out requestTag);
		var backdropTag = dto.BackdropImageTags?.FirstOrDefault();
		var parentBackdropTag = dto.ParentBackdropImageTags?.FirstOrDefault();

		var tag = "";
		if (hasRequestTag == true)
		{
			tag = $"{requestTag}";
		}
		else if (!string.IsNullOrEmpty(backdropTag))
		{
			tag = $"{backdropTag}";
		}
		else if (!string.IsNullOrEmpty(parentBackdropTag))
		{
			tag = $"{parentBackdropTag}";
		}

		if (type == ImageType.Backdrop && dto.Type is BaseItemDto_Type.Season && dto.ParentId is { } pid)
		{
			id = pid;
		}

		if (type == ImageType.Thumb && !hasRequestTag)
		{
			type = ImageType.Primary;
			if (dto.ImageTags.AdditionalData.TryGetValue($"{ImageType.Primary}", out var primaryTag))
			{
				tag = $"{primaryTag}";
			}
		}

		else if (type == ImageType.Primary && dto.Type == BaseItemDto_Type.Episode)
		{
			if (!string.IsNullOrEmpty(dto.SeriesPrimaryImageTag) && dto.SeriesId is { } seriesId)
			{
				id = seriesId;
				tag = $"{dto.SeriesPrimaryImageTag}";
			}
		}

		var uri = BaseUrl.AppendPathSegment($"/Items/{id}/Images/{type}");

		if (height is { } h)
		{
			uri.SetQueryParam("fillHeight", h);
		}

		if (!string.IsNullOrEmpty(tag))
		{
			uri.SetQueryParam("tag", tag);
		}


		return uri.ToUri();
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
				Item = dto,
			});

			_currentItem = dto;
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
		}
	}

	public async Task Progress(BaseItemDto dto, TimeSpan position)
	{
		try
		{
			await _jellyfinApiClient.Sessions.Playing.Progress.PostAsync(new PlaybackProgressInfo
			{
				Item = dto,
				PositionTicks = position.Ticks,
			});
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");

		}
	}

	public async Task Pause(BaseItemDto dto, TimeSpan? position = null)
	{
		try
		{
			await _jellyfinApiClient.Sessions.Playing.Progress.PostAsync(new PlaybackProgressInfo
			{
				Item = dto,
				IsPaused = true,
				PositionTicks = position?.Ticks
			});
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");

		}
	}

	public async Task Stop(BaseItemDto dto)
	{
		try
		{
			await _jellyfinApiClient.Sessions.Playing.Stopped.PostAsync(new PlaybackStopInfo
			{
				Item = dto
			});

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

		await Stop(_currentItem);
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
