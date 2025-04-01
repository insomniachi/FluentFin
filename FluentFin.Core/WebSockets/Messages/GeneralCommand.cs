﻿using System.Text.Json.Serialization;
using Jellyfin.Sdk.Generated.Models;

namespace FluentFin.Core.WebSockets.Messages;

public class GeneralCommand
{
	[JsonConverter(typeof(JsonStringEnumConverter<GeneralCommandType>))]
	public GeneralCommandType Name { get; set; }

	[JsonConverter(typeof(JsonGuidConverter))]
	public Guid ControllingUserId { get; set; }
	public Dictionary<string, string> Arguments { get; set; } = [];
}
