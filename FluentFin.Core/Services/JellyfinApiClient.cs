using FluentFin.Core.Contracts.Services;
using Flurl;
using Flurl.Http;
using Flurl.Http.Configuration;
using Jellyfin.Client;
using Jellyfin.Client.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FluentFin.Core.Services
{
	public class JellyfinApiClient(ILogger<JellyfinApiClient> logger) : IJellyfinClient
	{
		private UserDto _me = null!;
		private string _baseUrl = null!;
		private string _token = null!;
		private readonly DefaultJsonSerializer _serializer = new DefaultJsonSerializer(new JsonSerializerOptions
		{
			Converters =
			{
				new NullableGuidConveter(),
			}
		});

		public void Initialize(string baseUrl, AuthenticationResult authResult)
		{
			_me = authResult.User!;
			_baseUrl = baseUrl;
			_token = authResult.AccessToken!;
		}


		public async Task<BaseItemDtoQueryResult?> GetContinueWatching()
		{
			try
			{
				var result = await _baseUrl.AppendPathSegment($"/Users/{_me.Id}/Items/Resume")
					.SetQueryParams(new
					{
						Limit = 12,
						Recursive = true,
						Fields = "PrimaryImageAspectRatio",
						ImageTypeLimit = 1,
						EnableImageTypes = "Primary,Backdrop,Thumb",
						EnableTotalRecordCount = false,
						MediaTypes = "Video"
					})
					.WithHeader("X-Emby-Token", _token)
					.WithSettings(x => x.JsonSerializer = _serializer)
					.GetJsonAsync<BaseItemDtoQueryResult>();

				return result;

			}
			catch (Exception ex)
			{
				logger.LogError(ex, ex.Message);
				return null;
			}
		}
	}
}
