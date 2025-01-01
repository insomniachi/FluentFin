using FluentFin.Core.Contracts.Services;
using Jellyfin.Sdk.Generated.Models;
using Microsoft.Extensions.Logging;

namespace FluentFin.Core.Services;

public partial class JellyfinClient
{
	public async Task<BaseItemDtoQueryResult?> GetContinueWatching()
	{
		try
		{
			return await _jellyfinApiClient.UserItems.Resume.GetAsync(x =>
			{
				var query = x.QueryParameters;
				query.Limit = 12;
				query.Fields = [ItemFields.PrimaryImageAspectRatio];
				query.ImageTypeLimit = 1;
				query.EnableImageTypes = [ImageType.Primary, ImageType.Backdrop, ImageType.Thumb];
				query.EnableTotalRecordCount = false;
				query.MediaTypes = [MediaType.Video];
			});
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
			return null;
		}
	}

	public async Task<BaseItemDtoQueryResult?> GetNextUp()
	{
		try
		{
			return await _jellyfinApiClient.Shows.NextUp.GetAsync(x =>
			{
				var query = x.QueryParameters;
				query.Limit = 24;
				query.Fields = [ItemFields.PrimaryImageAspectRatio, ItemFields.DateCreated, ItemFields.Path, ItemFields.MediaSourceCount];
				query.UserId = UserId;
				query.ImageTypeLimit = 1;
				query.EnableImageTypes = [ImageType.Primary, ImageType.Backdrop, ImageType.Banner, ImageType.Thumb];
			});
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
			return null;
		}
	}

	public async IAsyncEnumerable<NamedDtoQueryResult> GetRecentItemsFromUserLibraries()
	{
		BaseItemDtoQueryResult? views = null;
		try
		{
			views = await _jellyfinApiClient.UserViews.GetAsync(x => x.QueryParameters.UserId = UserId);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
		}

		if (views is null or { Items: null })
		{
			yield break;
		}

		foreach (var library in views.Items)
		{
			if (library is null)
			{
				continue;
			}

			List<BaseItemDto>? info = [];
			try
			{
				info = await _jellyfinApiClient.Items.Latest.GetAsync(x =>
				{
					var query = x.QueryParameters;
					query.UserId = UserId;
					query.Limit = 16;
					query.ImageTypeLimit = 1;
					query.EnableImageTypes = [ImageType.Primary, ImageType.Backdrop, ImageType.Thumb];
					query.ParentId = library.Id;
				});
			}
			catch (Exception ex)
			{
				logger.LogError(ex, @"Unhandled exception");
			}

			if (info is not null and { Count: > 0 })
			{
				yield return new(library.Name ?? "", info);
			}
		}
	}

	public async IAsyncEnumerable<BaseItemDto> GetUserLibraries()
	{
		BaseItemDtoQueryResult? views = null;
		try
		{
			views = await _jellyfinApiClient.UserViews.GetAsync(x => x.QueryParameters.UserId = UserId);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
		}

		if (views is null or { Items: null })
		{
			yield break;
		}

		foreach (var item in views.Items)
		{
			yield return item;
		}
	}

	public async Task<UserItemDataDto?> ToggleMarkAsFavorite(BaseItemDto dto)
	{
		try
		{
			if (dto.Id is null)
			{
				return null;
			}

			return await _jellyfinApiClient.UserItems[dto.Id.Value].UserData.PostAsync(new UpdateUserItemDataDto
			{
				IsFavorite = !(dto.UserData?.IsFavorite ?? false)
			});
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
			return null;
		}
	}

	public async Task<UserItemDataDto?> ToggleMarkAsWatched(BaseItemDto dto)
	{
		try
		{
			if (dto.Id is null)
			{
				return null;
			}

			return await _jellyfinApiClient.UserItems[dto.Id.Value].UserData.PostAsync(new UpdateUserItemDataDto
			{
				Played = !(dto.UserData?.Played ?? false)
			});
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
			return null;
		}
	}

	public async Task<List<UserDto>> GetUsers()
	{
		try
		{
			return await _jellyfinApiClient.Users.GetAsync() ?? [];
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
			return [];
		}
	}

	public async Task<UserDto?> GetUser(Guid id)
	{
		try
		{
			return await _jellyfinApiClient.Users[id].GetAsync();
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
			return null;
		}
	}

	public async Task DeleteUser(UserDto user)
	{
		if(user.Id is not { } id)
		{
			return;
		}

		try
		{
			await _jellyfinApiClient.Users[id].DeleteAsync();
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
			return;
		}
	}

	public async Task UpdatePolicy(UserDto user, UserPolicy policy)
	{
		if (user.Id is not { } id)
		{
			return;
		}

		try
		{
			await _jellyfinApiClient.Users[id].Policy.PostAsync(policy);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
			return;
		}
	}
}
