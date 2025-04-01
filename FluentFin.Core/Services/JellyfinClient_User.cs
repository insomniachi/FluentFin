﻿using FluentFin.Core.Contracts.Services;
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
				query.EnableTotalRecordCount = false;
				query.DisableFirstEpisode = true;
				query.NextUpDateCutoff = TimeProvider.System.GetUtcNow();
				query.EnableResumable = false;
				query.EnableRewatching = false;
			});
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
			return null;
		}
	}

	public async IAsyncEnumerable<RecentItemDtoQueryResult> GetRecentItemsFromUserLibraries()
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

			if (library.CollectionType is BaseItemDto_CollectionType.Music)
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
				yield return new(library, [.. info]);
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

	public async Task SetIsFavorite(BaseItemDto dto, bool isFavorite)
	{
		try
		{
			if (dto.Id is null)
			{
				return;
			}

			if (isFavorite)
			{
				await _jellyfinApiClient.UserFavoriteItems[dto.Id.Value].PostAsync();
			}
			else
			{
				await _jellyfinApiClient.UserFavoriteItems[dto.Id.Value].DeleteAsync();
			}
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
			return;
		}
	}

	public async Task SetPlayed(BaseItemDto dto, bool played)
	{
		try
		{
			if (dto.Id is null)
			{
				return;
			}

			if (played)
			{
				await _jellyfinApiClient.UserPlayedItems[dto.Id.Value].PostAsync(x => x.QueryParameters.DatePlayed = TimeProvider.System.GetUtcNow());
			}
			else
			{
				await _jellyfinApiClient.UserPlayedItems[dto.Id.Value].DeleteAsync();
			}
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
			return;
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
		if (user.Id is not { } id)
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

	public async Task ChangePassword(UserDto user, string currentPassword, string newPassword)
	{
		if (user.Id is not { } id)
		{
			return;
		}

		try
		{
			await _jellyfinApiClient.Users.Password.PostAsync(new UpdateUserPassword
			{
				CurrentPassword = currentPassword,
				NewPw = newPassword,
			}, x => x.QueryParameters.UserId = id);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
			return;
		}
	}

	public async Task ResetPassword(UserDto user)
	{
		if (user.Id is not { } id)
		{
			return;
		}

		try
		{
			await _jellyfinApiClient.Users.Password.PostAsync(new UpdateUserPassword
			{
				ResetPassword = true,
			}, x => x.QueryParameters.UserId = id);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
			return;
		}
	}

	public async Task<bool> Authenticate(string code)
	{
		try
		{
			return await _jellyfinApiClient.QuickConnect.Authorize.PostAsync(x =>
			{
				var query = x.QueryParameters;
				query.UserId = UserId;
				query.Code = code;
			}) ?? false;
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
			return false;
		}
	}
}
