using Flurl.Http;
using Jellyfin.Sdk.Generated.Library.VirtualFolders;
using Jellyfin.Sdk.Generated.Models;
using Microsoft.Extensions.Logging;

namespace FluentFin.Core.Services;

public partial class JellyfinClient
{
	public async Task<BaseItemDtoQueryResult?> GetItems(BaseItemDto parent, bool recursive = false)
	{
		try
		{
			return await _jellyfinApiClient.Items.GetAsync(x =>
			{
				var query = x.QueryParameters;
				query.SortBy = [ItemSortBy.SortName];
				query.SortOrder = [SortOrder.Ascending];
				query.Recursive = recursive;
				query.Fields = [ItemFields.PrimaryImageAspectRatio, ItemFields.DateCreated, ItemFields.Overview, ItemFields.Tags, ItemFields.Genres];
				query.ImageTypeLimit = 1;
				query.EnableImageTypes = [ImageType.Primary, ImageType.Backdrop, ImageType.Banner, ImageType.Thumb];
				query.ParentId = parent.Id;
				query.IncludeItemTypes = parent.CollectionType switch
				{
					BaseItemDto_CollectionType.Movies => [BaseItemKind.Movie],
					BaseItemDto_CollectionType.Tvshows => [BaseItemKind.Series],
					_ => null
				};
			});
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
			return null;
		}
	}

	public async Task<BaseItemDto?> GetItem(Guid id)
	{
		try
		{
			return await _jellyfinApiClient.Items[id].GetAsync(x => x.QueryParameters.UserId = UserId);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
			return null;
		}
	}

	public async Task ResetProgress(Guid id)
	{
		var dto = await GetItem(id);
		if (dto is null or { UserData: null})
		{
			return;
		}

		dto.UserData.PlayedPercentage = 0;
		dto.UserData.PlaybackPositionTicks = 0;
		dto.UserData.LastPlayedDate = null;

		await _jellyfinApiClient.Items[id].PostAsync(dto);
	}

	public async Task<BaseItemDtoQueryResult?> GetSimilarItems(BaseItemDto dto)
	{
		if (dto.Id is not { } id)
		{
			return null;
		}

		try
		{
			return await _jellyfinApiClient.Items[id].Similar.GetAsync(x =>
			{
				x.QueryParameters.UserId = UserId;
				x.QueryParameters.Limit = 12;
			});
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
			return null;
		}
	}

	public async Task<BaseItemDtoQueryResult?> GetMediaFolders()
	{
		try
		{
			return await _jellyfinApiClient.Library.MediaFolders.GetAsync(x => x.QueryParameters.IsHidden = false);
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
			return null;
		}
	}

	public async Task<UserDto?> CreateUser(string username, string password)
	{
		try
		{
			return await _jellyfinApiClient.Users.New.PostAsync(new CreateUserByName
			{
				Name = username,
				Password = password
			});
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
			return null;
		}
	}

	public async Task<List<VirtualFolderInfo>> GetVirtualFolders()
	{
		try
		{
			return await _jellyfinApiClient.Library.VirtualFolders.GetAsync() ?? [];
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
			return [];
		}
	}

	public async Task CreateLibrary(string name, CollectionTypeOptions collectionType, LibraryOptions options)
	{
		try
		{
			await _jellyfinApiClient.Library.VirtualFolders.PostAsync(new AddVirtualFolderDto
			{
				LibraryOptions = options,
			}, x =>
			{
				var query = x.QueryParameters;
				query.RefreshLibrary = true;
				query.Name = name;
				query.CollectionType = collectionType;
			});
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
			return;
		}
	}

	public async Task DeleteLibrary(string name)
	{
		try
		{
			await _jellyfinApiClient.Library.VirtualFolders.DeleteAsync(x =>
			{
				var query = x.QueryParameters;
				query.RefreshLibrary = true;
				query.Name = name;
			});
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
			return;
		}
	}

	public async Task RenameLibrary(string name, string newName)
	{
		try
		{
			await _jellyfinApiClient.Library.VirtualFolders.Name.PostAsync(x =>
			{
				var query = x.QueryParameters;
				query.RefreshLibrary = true;
				query.Name = name;
				query.NewName = newName;
			});
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
			return;
		}
	}

	public async Task DeleteItem(BaseItemDto dto)
	{
		if (dto?.Id is not { } id)
		{
			return;
		}

		try
		{
			await _jellyfinApiClient.Items[id].DeleteAsync();
		}
		catch (Exception ex)
		{
			logger.LogError(ex, @"Unhandled exception");
			return;
		}
	}

	public async Task<Uri?> GetSplashScreen()
	{
		var endoint = _jellyfinApiClient.System.Configuration["branding"].ToGetRequestInformation();
		var options =  await AddApiKey(endoint.URI).GetJsonAsync<BrandingOptions>();

		if(options.SplashscreenEnabled is not true)
		{
			return null;
		}

		return AddApiKey(_jellyfinApiClient.Branding.Splashscreen.ToGetRequestInformation(x =>
		{
			var query = x.QueryParameters;
			query.Blur = 20;
			query.Height = 1080;
			query.Width = 1920;
			query.Format = Jellyfin.Sdk.Generated.Branding.Splashscreen.ImageFormat.Jpg;
		}).URI);
	}
}
