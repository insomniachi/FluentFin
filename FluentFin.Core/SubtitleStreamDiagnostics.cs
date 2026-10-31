using Flurl;
using Jellyfin.Sdk.Generated.Models;

namespace FluentFin.Core;

public static class SubtitleStreamDiagnostics
{
	/// <summary>One line for the log with what decides how a subtitle can be opened. The access token of the delivery url is not logged.</summary>
	public static string Describe(this MediaStream stream) =>
		$"#{stream.Index} codec={stream.Codec}, language={stream.Language}, title={stream.DisplayTitle}, isExternal={stream.IsExternal}, " +
		$"isTextSubtitle={stream.IsTextSubtitleStream}, delivery={stream.DeliveryMethod}, deliveryUrl={WithoutToken(stream.DeliveryUrl)}";

	/// <summary>The url without the access token that the server puts into the urls it hands out, so that it can be logged.</summary>
	public static string WithoutToken(string? url) =>
		string.IsNullOrEmpty(url) ? "" : new Url(url).RemoveQueryParams("ApiKey", "api_key").ToString();
}
