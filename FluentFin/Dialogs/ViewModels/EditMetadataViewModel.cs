using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentFin.Core.Contracts.Services;
using Jellyfin.Sdk.Generated.Models;

namespace FluentFin.Dialogs.ViewModels;

public partial class EditMetadataViewModel : ObservableObject, IHandleClose, IBaseItemDialogViewModel
{
	private readonly IJellyfinClient _jellyfinClient;

	public EditMetadataViewModel(IJellyfinClient jellyfinClient)
	{
		_jellyfinClient = jellyfinClient;
	}

	[ObservableProperty]
	public partial string? Path { get; set; }

	[ObservableProperty]
	public partial string? Title { get; set; }

	[ObservableProperty]
	public partial string? OriginalTitle { get; set; }

	[ObservableProperty]
	public partial string? SortTitle { get; set; }

	[ObservableProperty]
	public partial DateTimeOffset? DateAdded { get; set; }

	[ObservableProperty]
	public partial string? Status { get; set; }

	[ObservableProperty]
	public partial double? CommunityRating { get; set; }

	[ObservableProperty]
	public partial string? Overview { get; set; }

	[ObservableProperty]
	public partial DateTimeOffset? ReleaseDate { get; set; }

	[ObservableProperty]
	public partial double? Year { get; set; }

	[ObservableProperty]
	public partial DateTimeOffset? EndDate { get; set; }

	[ObservableProperty]
	public partial TimeSpan? AirTime { get; set; }

	[ObservableProperty]
	public partial string? DisplayOrder { get; set; }

	[ObservableProperty]
	public partial string? ParentalRating { get; set; }

	[ObservableProperty]
	public partial string? CustomRating { get; set; }

	[ObservableProperty]
	public partial string? OriginalAspectRatio { get; set; }

	[ObservableProperty]
	public partial BaseItemDto_Video3DFormat? Video3DFormat { get; set; }

	[ObservableProperty]
	public partial List<string?> RatingValues { get; set; } = [];

	[ObservableProperty]
	public partial List<BaseItemDto_Video3DFormat?> Video3DFormatValues { get; set; } = [];

	[ObservableProperty]
	public partial List<KeyValueViewModel> ExternalIds { get; set; } = [];

	public ObservableCollection<string> Genres { get; } = [];

	public List<string?> StatusValues { get; } = [null, "Ended", "Continuing", "Not yet released"];

	public MetadataEditorInfo? MetadataEditorInfo { get; set; }


	public bool CanClose { get; set; }

	public BaseItemDto? Item { get; set; }

	public async Task Initialize(BaseItemDto dto)
	{
		if (dto.Id is not { } id)
		{
			return;
		}

		Item = await _jellyfinClient.GetItem(id);

		if (Item is null)
		{
			return;
		}

		MetadataEditorInfo = await _jellyfinClient.GetMetadataEditorInfo(dto);
		if (MetadataEditorInfo is { ParentalRatingOptions: not null })
		{
			RatingValues = [null, .. MetadataEditorInfo.ParentalRatingOptions.Select(x => x.Name), Item.OfficialRating];
		}

		Video3DFormatValues = [null, .. Enum.GetValues<BaseItemDto_Video3DFormat>()];

		if (MetadataEditorInfo is { ExternalIdInfos: not null })
		{
			ExternalIds = MetadataEditorInfo.ExternalIdInfos
				.Where(x => !string.IsNullOrEmpty(x.Name) || !string.IsNullOrEmpty(x.Key))
				.Select(x => new KeyValueViewModel(x.Name!, x.Key!, x.Type) { Value = GetValue(Item.ProviderIds, x) }).ToList();
		}


		Path = Item.Path;
		Title = Item.Name;
		OriginalTitle = Item.OriginalTitle;
		SortTitle = Item.SortName;
		DateAdded = Item.DateCreated;
		Status = Item.Status;
		CommunityRating = Item.CommunityRating;
		Overview = Item.Overview;
		ReleaseDate = Item.PremiereDate ?? Item.StartDate;
		Year = Item.ProductionYear;
		EndDate = Item.EndDate;
		ParentalRating = Item.OfficialRating;
		CustomRating = Item.CustomRating;
		OriginalAspectRatio = Item.AspectRatio;
		Video3DFormat = Item.Video3DFormat;

		foreach (var item in Item.Genres ?? [])
		{
			Genres.Add(item);
		}
	}

	[RelayCommand]
	public void DeleteGenre(string genre)
	{
		Genres.Remove(genre);
	}

	[RelayCommand]
	public void AddGenre(string genre)
	{
		if (Genres.Contains(genre))
		{
			return;
		}

		Genres.Add(genre);
	}


	[RelayCommand]
	public void Reset()
	{
		Title = null;
		OriginalTitle = null;
		SortTitle = null;
		DateAdded = null;
		Status = null;
		CommunityRating = null;
		Overview = null;
		ReleaseDate = null;
		Year = null;
		EndDate = null;
		ParentalRating = null;
		CustomRating = null;
		OriginalAspectRatio = null;
		Video3DFormat = null;

		foreach (var item in ExternalIds)
		{
			item.Value = null;
		}

		Genres.Clear();
	}

	[RelayCommand]
	public async Task Save()
	{
		if (Item is null)
		{
			return;
		}

		Item.Name = Title;
		Item.OriginalTitle = OriginalTitle;
		Item.SortName = SortTitle;
		Item.DateCreated = DateAdded;
		Item.Status = Status;
		Item.CommunityRating = (float?)CommunityRating;
		Item.Overview = Overview;
		Item.ProductionYear = (int?)Year;
		Item.EndDate = EndDate;
		Item.OfficialRating = ParentalRating;
		Item.CustomRating = CustomRating;
		Item.AspectRatio = OriginalAspectRatio;
		Item.Video3DFormat = Video3DFormat;

		foreach (var item in ExternalIds)
		{
			if (Item.ProviderIds?.AdditionalData?.ContainsKey(item.Key) == true)
			{
				Item.ProviderIds.AdditionalData[item.Key] = item.Value;
			}
		}

		Item.Genres = [.. Genres];

		await _jellyfinClient.UpdateMetadata(Item);
	}

	private static string? GetValue(BaseItemDto_ProviderIds? ids, ExternalIdInfo info)
	{
		if (ids is null)
		{
			return null;
		}

		return ids.AdditionalData.TryGetValue(info.Key, out var value) ? $"{value}" : null;
	}
}
