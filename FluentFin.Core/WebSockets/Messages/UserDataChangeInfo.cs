using System.Text.Json.Serialization;

namespace FluentFin.Core.WebSockets.Messages;

public class UserDataChangeInfo
{
	[JsonConverter(typeof(JsonNullableGuidConverter))]
	public Guid? UserId { get; set; }

	public UserItemDataDto[] UserDataList { get; set; } = [];
}

public class UserItemDataDto
{
	public bool? IsFavorite { get; set; }
	public string? ItemId { get; set; }
	public string? Key { get; set; }
	public DateTimeOffset? LastPlayedDate { get; set; }
	public bool? Likes { get; set; }
	public long? PlaybackPositionTicks { get; set; }
	public int? PlayCount { get; set; }
	public bool? Played { get; set; }
	public double? PlayedPercentage { get; set; }
	public double? Rating { get; set; }
	public int? UnplayedItemCount { get; set; }
}