using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using FluentFin.Core;
using FluentFin.Core.Services;
using Flurl;
using Flurl.Http;
using Microsoft.Extensions.Logging;

namespace FluentFin.Plugins.Playback_Reporting;

public static class PlaybackReportingHelper
{
	public static async Task<List<UserActivity>> GetUserActivity(int days, DateTimeOffset endDate)
	{
		try
		{
			var stream = await SessionInfo.BaseUrl
				.AppendPathSegment("/user_usage_stats/user_activity")
				.SetQueryParams(new
				{
					days,
					end_date = endDate,
					api_key = SessionInfo.AccessToken
				})
				.GetStreamAsync();

			var node = JsonNode.Parse(stream)?.AsArray() ?? [];

			return node.Deserialize<List<UserActivity>>() ?? [];
		}
		catch (Exception ex)
		{
			Locator.GetService<ILogger<JellyfinClient>>().LogError(ex, "Unhandled exception");
			return [];
		}
	}
}

public class UserActivity
{
	[JsonPropertyName("latest_date")]
	public DateTime LatestDate { get; set; }

	[JsonPropertyName("user_id")]
	public string UserId { get; set; } = "";

	[JsonPropertyName("total_count")]
	public int TotalCount { get; set; }

	[JsonPropertyName("total_time")]
	public int TotalTime { get; set; }

	[JsonPropertyName("item_name")]
	public string ItemName { get; set; } = "";

	[JsonPropertyName("client_name")]
	public string ClientName { get; set; } = "";

	[JsonPropertyName("user_name")]
	public string UserName { get; set; } = "";

	[JsonPropertyName("has_image")]
	public bool HasImage { get; set; }

	[JsonPropertyName("last_seen")]
	public string LastSeen { get; set; } = "";

	[JsonPropertyName("total_play_time")]
	public string TotalPlayTime { get; set; } = "";
}