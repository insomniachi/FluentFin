using System.Text.RegularExpressions;
using Flurl;

namespace FluentFin.Core;

public static partial class ApiKeyExtensions
{
	public const string Mask = "***";

	/// <summary>
	/// Sets the access token on a url. Any token already on it is replaced, Jellyfin puts one in the transcoding urls it
	/// returns and a second <c>ApiKey</c> makes the server read the token as "token,token" and answer 401.
	/// </summary>
	public static Uri WithApiKey(this Uri uri, string token)
		=> new Url(uri).RemoveQueryParams("ApiKey", "api_key").SetQueryParam("ApiKey", token).ToUri();

	/// <summary>
	/// Keeps the <c>ApiKey</c> parameter in urls and in text that contains urls (player error messages repeat the url),
	/// but replaces its value so that the access token does not end up in logs.
	/// </summary>
	public static string MaskApiKey(this string? text)
		=> string.IsNullOrEmpty(text) ? text ?? "" : ApiKeyValue().Replace(text, Mask);

	public static string MaskApiKey(this Uri uri) => uri.ToString().MaskApiKey();

	[GeneratedRegex("(?<=(?:ApiKey|api_key)=)[^&\\s'\"]+", RegexOptions.IgnoreCase)]
	private static partial Regex ApiKeyValue();
}
