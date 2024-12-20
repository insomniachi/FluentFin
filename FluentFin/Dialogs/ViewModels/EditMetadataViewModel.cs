using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentFin.Core.Contracts.Services;
using Jellyfin.Sdk.Generated.Models;

namespace FluentFin.Dialogs.ViewModels;

public partial class EditMetadataViewModel : ObservableObject
{
	private readonly IJellyfinClient _jellyfinClient;

	public EditMetadataViewModel(IJellyfinClient jellyfinClient)
	{
		_jellyfinClient = jellyfinClient;

		var dto = new BaseItemDto();
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
	public partial string? ParentRating { get; set; }

	[ObservableProperty]
	public partial string? DisplayOrder { get; set; }

	public List<string?> StatusValues { get; } = [null, "Ended", "Continuing", "Not yet released"];

	public bool CanClose { get; set; }

	public BaseItemDto? Dto { get; set; }

	public async Task Initialize(Guid id)
	{
		Dto = await _jellyfinClient.GetItem(id);

		if(Dto is null)
		{
			return;
		}

		Path = Dto.Path;
		Title = Dto.Name;
		OriginalTitle = Dto.OriginalTitle;
		SortTitle = Dto.SortName;
		DateAdded = Dto.DateCreated;
		Status = Dto.Status;
		CommunityRating = Dto.CommunityRating;
		Overview = Dto.Overview;
		ReleaseDate = Dto.PremiereDate ?? Dto.StartDate;
		Year = Dto.ProductionYear;
		EndDate = Dto.EndDate;
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
	}

	[RelayCommand]
	public async Task Save()
	{
		if(Dto is null)
		{
			return;
		}	

		Dto.Name = Title;
		Dto.OriginalTitle = OriginalTitle;
		Dto.SortName = SortTitle;
		Dto.DateCreated = DateAdded;
		Dto.Status = Status;
		Dto.CommunityRating = (float?)CommunityRating;
		Dto.Overview = Overview;
		Dto.ProductionYear = (int?)Year;
		Dto.EndDate = EndDate;

		await _jellyfinClient.UpdateMetadata(Dto);
	}
}
