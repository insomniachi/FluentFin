using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentFin.Contracts.ViewModels;
using FluentFin.Core.Contracts.Services;
using Jellyfin.Sdk.Generated.Models;
using System.Collections.ObjectModel;

namespace FluentFin.Dialogs.ViewModels;

public partial class ManageLibraryViewModel(IJellyfinClient jellyfinClient) : ObservableObject, IHandleClose
{
	private VirtualFolderInfo? _info;

	[ObservableProperty]
	public partial string Name { get; set; }

	[ObservableProperty]
	public partial ObservableCollection<string> Locations { get; set; } = [];

	[ObservableProperty]
	public partial bool IsEnabled { get; set; }

	[ObservableProperty]
	public partial string SeasonZeroDisplayName { get; set; } = "";

	[ObservableProperty]
	public partial bool EnableEmbeddedTitles { get; set; }

	[ObservableProperty]
	public partial bool EnableEmbeddedExtrasTitles { get; set; }

	[ObservableProperty]
	public partial bool EnableEmbeddedEpisodeInfo { get; set; }

	[ObservableProperty]
	public partial LibraryOptions_AllowEmbeddedSubtitles? AllowEmbeddedSubtitles { get; set; }

	[ObservableProperty]
	public partial bool EnableRealtimeMonitor { get; set; }

	[ObservableProperty]
	public partial int AutomaticRefreshIntervalDays { get; set; }

	[ObservableProperty]
	public partial bool AutomaticallyAddToCollection { get; set; }

	[ObservableProperty]
	public partial bool EnableTrickplayImageExtraction { get; set; }

	[ObservableProperty]
	public partial bool ExtractTrickplayImagesDuringLibraryScan { get; set; }

	[ObservableProperty]
	public partial bool SaveTrickplayWithMedia { get; set; }

	[ObservableProperty]
	public partial bool EnableChapterImageExtraction { get; set; }

	[ObservableProperty]
	public partial bool ExtractChapterImagesDuringLibraryScan { get; set; }

	[ObservableProperty]
	public partial bool RequirePerfectSubtitleMatch { get; set; }

	[ObservableProperty]
	public partial bool SkipSubtitlesIfEmbeddedSubtitlesPresent { get; set; }

	[ObservableProperty]
	public partial bool SkipSubtitlesIfAudioTrackMatches { get; set; }

	[ObservableProperty]
	public partial bool SaveSubtitlesWithMedia { get; set; }
	public bool CanClose { get; set; }

	[ObservableProperty]
	public partial LibraryOptionsResultDto? LibraryOptionsInfo { get; set; }

	public ObservableCollection<MetadataFetcher> SeriesMetadataFetchers { get; } = [];
	public ObservableCollection<MetadataFetcher> SeasonMetadataFetchers { get; } = [];
	public ObservableCollection<MetadataFetcher> EpisodeMetadataFetchers { get; } = [];
	public ObservableCollection<MetadataFetcher> SeriesImageFetchers { get; } = [];
	public ObservableCollection<MetadataFetcher> SeasonImageFetchers { get; } = [];
	public ObservableCollection<MetadataFetcher> EpisodeImageFetchers { get; } = [];


	public async Task Initialize(VirtualFolderInfo virtualFolder)
	{
		if(virtualFolder.LibraryOptions is not { } libraryOptions)
		{
			return;
		}

		if(virtualFolder.CollectionType is not null)
		{
			LibraryOptionsInfo = await jellyfinClient.GetAvailableInfo((Jellyfin.Sdk.Generated.Libraries.AvailableOptions.CollectionType)(int)virtualFolder.CollectionType, false);
		}


		_info = virtualFolder;

		Name = _info.Name ?? "";
		Locations = new(_info.Locations ?? []);
		IsEnabled = libraryOptions.Enabled ?? false;
		SeasonZeroDisplayName = libraryOptions.SeasonZeroDisplayName ?? string.Empty;
		EnableEmbeddedTitles = libraryOptions.EnableEmbeddedTitles ?? false;
		EnableEmbeddedExtrasTitles = libraryOptions.EnableEmbeddedExtrasTitles ?? false;
		EnableEmbeddedEpisodeInfo = libraryOptions.EnableEmbeddedEpisodeInfos ?? false;
		AllowEmbeddedSubtitles = libraryOptions.AllowEmbeddedSubtitles;
		EnableRealtimeMonitor = libraryOptions.EnableRealtimeMonitor ?? false;
		AutomaticRefreshIntervalDays = libraryOptions.AutomaticRefreshIntervalDays ?? 0;
		AutomaticallyAddToCollection = libraryOptions.AutomaticallyAddToCollection ?? false;
		EnableTrickplayImageExtraction = libraryOptions.EnableTrickplayImageExtraction ?? false;
		ExtractTrickplayImagesDuringLibraryScan = libraryOptions.ExtractTrickplayImagesDuringLibraryScan ?? false;
		SaveTrickplayWithMedia = libraryOptions.SaveTrickplayWithMedia ?? false;
		EnableChapterImageExtraction = libraryOptions.EnableChapterImageExtraction ?? false;
		ExtractChapterImagesDuringLibraryScan = libraryOptions.ExtractChapterImagesDuringLibraryScan ?? false;
		RequirePerfectSubtitleMatch = libraryOptions.RequirePerfectSubtitleMatch ?? false;
		SkipSubtitlesIfEmbeddedSubtitlesPresent = libraryOptions.SkipSubtitlesIfEmbeddedSubtitlesPresent ?? false;
		SkipSubtitlesIfAudioTrackMatches = libraryOptions.SkipSubtitlesIfAudioTrackMatches ?? false;
		SaveSubtitlesWithMedia = libraryOptions.SaveSubtitlesWithMedia ?? false;

		PopulateMetadataFetcher("Series", libraryOptions, SeriesMetadataFetchers);
		PopulateMetadataFetcher("Season", libraryOptions, SeasonMetadataFetchers);
		PopulateMetadataFetcher("Episode", libraryOptions, EpisodeMetadataFetchers);
		PopulateImageFetcher("Series", libraryOptions, SeriesImageFetchers);
		PopulateImageFetcher("Season", libraryOptions, SeasonImageFetchers);
		PopulateImageFetcher("Episode", libraryOptions, EpisodeImageFetchers);

		return;
	}

	[RelayCommand]
	private async Task Reset()
	{
		if(_info is null)
		{
			return;
		}

		await Initialize(_info);
	}

	private static void PopulateMetadataFetcher(string type, LibraryOptions options, ObservableCollection<MetadataFetcher> target)
	{
		target.Clear();
		var typeOption = options.TypeOptions?.FirstOrDefault(x => x.Type == type);
		if (typeOption is not null)
		{
			foreach (var (index, item) in (typeOption.MetadataFetcherOrder ?? []).Index())
			{
				target.Add(new MetadataFetcher(item, target)
				{
					IsSelected = typeOption.MetadataFetchers?.Any(x => x == item) == true,
				});
			}

			target[0].CanMoveUp = false;
			target[^1].CanMoveDown = false;
		}
	}

	private static void PopulateImageFetcher(string type, LibraryOptions options, ObservableCollection<MetadataFetcher> target)
	{
		target.Clear();
		var typeOption = options.TypeOptions?.FirstOrDefault(x => x.Type == type);
		if (typeOption is not null)
		{
			foreach (var (index, item) in (typeOption.ImageFetcherOrder ?? []).Index())
			{
				target.Add(new MetadataFetcher(item, target)
				{
					IsSelected = typeOption.ImageFetchers?.Any(x => x == item) == true,
				});
			}

			target[0].CanMoveUp = false;
			target[^1].CanMoveDown = false;
		}
	}
}

public partial class MetadataFetcher(string name, ObservableCollection<MetadataFetcher> parentCollection) : ObservableObject
{
	private readonly ObservableCollection<MetadataFetcher> _parentCollection = parentCollection;

	public string Name { get; } = name;

	[ObservableProperty]
	public partial bool IsSelected { get; set; }

	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(MoveUpCommand))]
	public partial bool CanMoveUp { get; set; } = true;

	[ObservableProperty]
	[NotifyCanExecuteChangedFor(nameof(MoveDownCommand))]
	public partial bool CanMoveDown { get; set; } = true;

	[RelayCommand(CanExecute = nameof(CanMoveUp))]
	private void MoveUp()
	{
		var index = _parentCollection.IndexOf(this);
		_parentCollection.Remove(this);
		var newIndex = index - 1;
		_parentCollection.Insert(newIndex, this);
		CanMoveUp = newIndex > 0;
	}


	[RelayCommand(CanExecute = nameof(CanMoveDown))]
	private void MoveDown()
	{
		var index = _parentCollection.IndexOf(this);
		_parentCollection.Remove(this);
		var newIndex = index + 1;
		_parentCollection.Insert(newIndex, this);
		CanMoveDown = newIndex < _parentCollection.Count - 1;
	}
}
