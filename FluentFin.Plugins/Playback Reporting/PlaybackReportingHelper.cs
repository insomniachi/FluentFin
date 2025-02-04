using System;
using System.Collections.Generic;
using System.IO;
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

	public static async Task<List<PlayActivity>> GetPlayActivity(int days, DateTimeOffset endDate)
	{
		try
		{
			var stream = await SessionInfo.BaseUrl
				.AppendPathSegment("/user_usage_stats/PlayActivity")
				.SetQueryParams(new
				{
					days,
					end_date = endDate,
					api_key = SessionInfo.AccessToken,
					filter = "Episode,Movie,Audio,Series",
					dataType = "count"
				})
				.GetStreamAsync();

			var node = JsonNode.Parse(stream)?.AsArray() ?? [];

			return node.Deserialize<List<PlayActivity>>() ?? [];
		}
		catch (Exception ex)
		{
			Locator.GetService<ILogger<JellyfinClient>>().LogError(ex, "Unhandled exception");
			return [];
		}
	}

	public static async Task<List<ActivityBreakdown>> GetBreakdown(string type, int days, DateTimeOffset endDate)
	{
		try
		{
			Stream stream;

			if (type is "GetTvShowsReport" or "MoviesReport")
			{
				stream = await SessionInfo.BaseUrl
					.AppendPathSegment($"/user_usage_stats/{type}")
					.SetQueryParams(new
					{
						days,
						end_date = endDate,
						api_key = SessionInfo.AccessToken
					})
					.GetStreamAsync();
			}
			else
			{
				stream = await SessionInfo.BaseUrl
					.AppendPathSegment($"/user_usage_stats/{type}/BreakdownReport")
					.SetQueryParams(new
					{
						days,
						end_date = endDate,
						api_key = SessionInfo.AccessToken
					})
					.GetStreamAsync();
			}

			var node = JsonNode.Parse(stream)?.AsArray() ?? [];
			return node.Deserialize<List<ActivityBreakdown>>() ?? [];
		}
		catch (Exception ex)
		{
			Locator.GetService<ILogger<JellyfinClient>>().LogError(ex, "Unhandled exception");
			return [];
		}
	}

	public static async Task<List<ActivityBreakdown>> GetBreakdownReport(string type, int days, DateTimeOffset endDate)
	{
		try
		{
			var stream = await SessionInfo.BaseUrl
				.AppendPathSegment($"/user_usage_stats/{type}")
				.SetQueryParams(new
				{
					days,
					end_date = endDate,
					api_key = SessionInfo.AccessToken
				})
				.GetStreamAsync();
			var node = JsonNode.Parse(stream)?.AsArray() ?? [];
			return node.Deserialize<List<ActivityBreakdown>>() ?? [];
		}
		catch (Exception ex)
		{
			Locator.GetService<ILogger<JellyfinClient>>().LogError(ex, "Unhandled exception");
			return [];
		}
	}

	public static async Task<Dictionary<string, int>> GetHourlyReport(int days, DateTimeOffset endDate)
	{
		try
		{
			return await SessionInfo.BaseUrl
				.AppendPathSegment("/user_usage_stats/HourlyReport")
				.SetQueryParams(new
				{
					days,
					end_date = endDate,
					api_key = SessionInfo.AccessToken,
					filter = "Episode,Movie,Audio,Series",
				})
				.GetJsonAsync<Dictionary<string, int>>();
		}
		catch (Exception ex)
		{
			Locator.GetService<ILogger<JellyfinClient>>().LogError(ex, "Unhandled exception");
			return [];
		}
	}
}

public class PlayActivity
{
	[JsonPropertyName("user_id")]
	public string UserId { get; set; } = "";

	[JsonPropertyName("user_name")]
	public string UserName { get; set; } = "";

	[JsonPropertyName("user_usage")]
	public Dictionary<string, int> UserUsage { get; set; } = [];
}

public class ActivityBreakdown
{
	[JsonPropertyName("label")]
	public string Label { get; set; } = "";

	[JsonPropertyName("count")]
	public int Count { get; set; }

	[JsonPropertyName("time")]
	public long Time { get; set; }
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