using System.Collections.ObjectModel;
using System.Reactive.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevWinUI;
using FluentFin.Core.Contracts.Services;
using Jellyfin.Sdk.Generated.Models;
using ReactiveUI;

namespace FluentFin.Dialogs.ViewModels;

public partial class ManageLibraryViewModel(IJellyfinClient jellyfinClient) : ObservableObject, IHandleClose
{
	private VirtualFolderInfo? _info;

	[ObservableProperty]
	public partial string Name { get; set; }

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

	[ObservableProperty]
	public partial bool SaveLocalMetadata { get; set; }

	[ObservableProperty]
	public partial List<CultureDto> SubtitleLanguages { get; set; }

	[ObservableProperty]
	public partial CultureDto? PreferredMetadataLanguage { get; set; }

	[ObservableProperty]
	public partial CountryInfo? MetadataCountryCode { get; set; }

	[ObservableProperty]
	public partial bool IsMovieFolder { get; set; }

	[ObservableProperty]
	public partial bool IsSeriesFolder { get; set; }

	[ObservableProperty]
	public partial List<CultureDto?> Cultures { get; set; } = [];

	[ObservableProperty]
	public partial List<CountryInfo?> Countries { get; set; } = [];

	[ObservableProperty]
	public partial bool IsCreateMode { get; set; }


	[ObservableProperty]
	public partial Jellyfin.Sdk.Generated.Libraries.AvailableOptions.CollectionType? FolderCollectionType { get; set; }

	public ObservableCollection<MetadataFetcher> SeriesMetadataFetchers { get; } = [];
	public ObservableCollection<MetadataFetcher> SeasonMetadataFetchers { get; } = [];
	public ObservableCollection<MetadataFetcher> EpisodeMetadataFetchers { get; } = [];
	public ObservableCollection<MetadataFetcher> MovieMetadataFetchers { get; } = [];
	public ObservableCollection<MetadataFetcher> SeriesImageFetchers { get; } = [];
	public ObservableCollection<MetadataFetcher> SeasonImageFetchers { get; } = [];
	public ObservableCollection<MetadataFetcher> EpisodeImageFetchers { get; } = [];
	public ObservableCollection<MetadataFetcher> MovieImageFetcher { get; } = [];
	public ObservableCollection<MetadataFetcher> SubtitleFetchers { get; } = [];
	public ObservableCollection<string> Locations { get; } = [];
	public List<int> RefreshIntervals { get; set; } = [0, 60, 90];


	public async Task Initialize()
	{
		IsCreateMode = true;
		_info = new VirtualFolderInfo { LibraryOptions = new LibraryOptions() };

		Cultures = [null, .. await jellyfinClient.GetCultures()];
		Countries = [null, .. await jellyfinClient.GetCountries()];

		this.WhenAnyValue(x => x.FolderCollectionType)
			.WhereNotNull()
			.SelectMany(type => jellyfinClient.GetAvailableInfo(type!.Value, true))
			.WhereNotNull()
			.ObserveOn(RxApp.MainThreadScheduler)
			.Subscribe(value =>
			{
				if (FolderCollectionType == Jellyfin.Sdk.Generated.Libraries.AvailableOptions.CollectionType.Tvshows)
				{
					PopulateMetadataFetcher("Series", value, SeriesMetadataFetchers);
					PopulateMetadataFetcher("Season", value, SeasonMetadataFetchers);
					PopulateMetadataFetcher("Episode", value, EpisodeMetadataFetchers);

					PopulateImageFetcher("Series", value, SeriesImageFetchers);
					PopulateImageFetcher("Season", value, SeasonImageFetchers);
					PopulateImageFetcher("Episode", value, EpisodeImageFetchers);
				}
				else if (FolderCollectionType == Jellyfin.Sdk.Generated.Libraries.AvailableOptions.CollectionType.Movies)
				{
					PopulateMetadataFetcher("Movie", value, MovieMetadataFetchers);

					PopulateImageFetcher("Movie", value, MovieImageFetcher);
				}

				IsMovieFolder = FolderCollectionType == Jellyfin.Sdk.Generated.Libraries.AvailableOptions.CollectionType.Movies;
				IsSeriesFolder = FolderCollectionType == Jellyfin.Sdk.Generated.Libraries.AvailableOptions.CollectionType.Tvshows;
			});

	}

	public async Task Initialize(VirtualFolderInfo virtualFolder)
	{
		IsCreateMode = false;
		if (virtualFolder.LibraryOptions is not { } options)
		{
			return;
		}

		Cultures = [null, .. await jellyfinClient.GetCultures()];
		Countries = [null, .. await jellyfinClient.GetCountries()];

		_info = virtualFolder;
		FolderCollectionType = (Jellyfin.Sdk.Generated.Libraries.AvailableOptions.CollectionType)(int)_info.CollectionType!;
		IsMovieFolder = _info.CollectionType == VirtualFolderInfo_CollectionType.Movies;
		IsSeriesFolder = _info.CollectionType == VirtualFolderInfo_CollectionType.Tvshows;

		Name = _info.Name ?? "";
		Locations.AddRange(_info.Locations ?? []);
		IsEnabled = options.Enabled ?? false;
		SeasonZeroDisplayName = options.SeasonZeroDisplayName ?? string.Empty;
		EnableEmbeddedTitles = options.EnableEmbeddedTitles ?? false;
		EnableEmbeddedExtrasTitles = options.EnableEmbeddedExtrasTitles ?? false;
		EnableEmbeddedEpisodeInfo = options.EnableEmbeddedEpisodeInfos ?? false;
		AllowEmbeddedSubtitles = options.AllowEmbeddedSubtitles;
		EnableRealtimeMonitor = options.EnableRealtimeMonitor ?? false;
		AutomaticRefreshIntervalDays = options.AutomaticRefreshIntervalDays ?? 0;
		AutomaticallyAddToCollection = options.AutomaticallyAddToCollection ?? false;
		EnableTrickplayImageExtraction = options.EnableTrickplayImageExtraction ?? false;
		ExtractTrickplayImagesDuringLibraryScan = options.ExtractTrickplayImagesDuringLibraryScan ?? false;
		SaveTrickplayWithMedia = options.SaveTrickplayWithMedia ?? false;
		EnableChapterImageExtraction = options.EnableChapterImageExtraction ?? false;
		ExtractChapterImagesDuringLibraryScan = options.ExtractChapterImagesDuringLibraryScan ?? false;
		RequirePerfectSubtitleMatch = options.RequirePerfectSubtitleMatch ?? false;
		SkipSubtitlesIfEmbeddedSubtitlesPresent = options.SkipSubtitlesIfEmbeddedSubtitlesPresent ?? false;
		SkipSubtitlesIfAudioTrackMatches = options.SkipSubtitlesIfAudioTrackMatches ?? false;
		SaveSubtitlesWithMedia = options.SaveSubtitlesWithMedia ?? false;
		SaveLocalMetadata = options.SaveLocalMetadata ?? false;
		;
		PreferredMetadataLanguage = Cultures.FirstOrDefault(x => x?.TwoLetterISOLanguageName == options.PreferredMetadataLanguage);
		MetadataCountryCode = Countries.FirstOrDefault(x => x?.TwoLetterISORegionName == options.MetadataCountryCode);

		if (FolderCollectionType == Jellyfin.Sdk.Generated.Libraries.AvailableOptions.CollectionType.Tvshows)
		{
			PopulateMetadataFetcher("Series", options, SeriesMetadataFetchers);
			PopulateMetadataFetcher("Season", options, SeasonMetadataFetchers);
			PopulateMetadataFetcher("Episode", options, EpisodeMetadataFetchers);

			PopulateImageFetcher("Series", options, SeriesImageFetchers);
			PopulateImageFetcher("Season", options, SeasonImageFetchers);
			PopulateImageFetcher("Episode", options, EpisodeImageFetchers);
		}
		else if (FolderCollectionType == Jellyfin.Sdk.Generated.Libraries.AvailableOptions.CollectionType.Movies)
		{
			PopulateMetadataFetcher("Movie", options, MovieMetadataFetchers);

			PopulateImageFetcher("Movie", options, MovieImageFetcher);
		}

		IEnumerable<MetadataFetcher> subtitleFetchers = options.SubtitleFetcherOrder?.Select(x => new MetadataFetcher(x, SubtitleFetchers) { IsSelected = true, CanMoveDown = true, CanMoveUp = true }) ?? [];
		SubtitleFetchers.AddRange(subtitleFetchers);
		SubtitleFetchers[0].CanMoveUp = false;
		SubtitleFetchers[^1].CanMoveDown = false;

		var selectedSubtitleLanguages = new List<CultureDto>();
		foreach (var item in options.SubtitleDownloadLanguages ?? [])
		{
			if (Cultures.FirstOrDefault(x => x?.ThreeLetterISOLanguageName == item) is { } culture)
			{
				selectedSubtitleLanguages.Add(culture);
			}
		}
		SubtitleLanguages = selectedSubtitleLanguages;

		return;
	}

	[RelayCommand]
	private async Task Save()
	{
		if (_info is not { ItemId: not null })
		{
			return;
		}

		if (_info.LibraryOptions is not { } options)
		{
			return;
		}

		options.PathInfos = [.. Locations.Select(x => new MediaPathInfo { Path = x })];
		options.Enabled = IsEnabled;
		options.SeasonZeroDisplayName = SeasonZeroDisplayName;
		options.EnableEmbeddedTitles = EnableEmbeddedTitles;
		options.EnableEmbeddedExtrasTitles = EnableEmbeddedExtrasTitles;
		options.EnableEmbeddedEpisodeInfos = EnableEmbeddedEpisodeInfo;
		options.AllowEmbeddedSubtitles = AllowEmbeddedSubtitles;
		options.EnableRealtimeMonitor = EnableRealtimeMonitor;
		options.AutomaticRefreshIntervalDays = AutomaticRefreshIntervalDays;
		options.AutomaticallyAddToCollection = AutomaticallyAddToCollection;
		options.EnableTrickplayImageExtraction = EnableTrickplayImageExtraction;
		options.ExtractTrickplayImagesDuringLibraryScan = ExtractTrickplayImagesDuringLibraryScan;
		options.SaveTrickplayWithMedia = SaveTrickplayWithMedia;
		options.EnableChapterImageExtraction = EnableChapterImageExtraction;
		options.ExtractChapterImagesDuringLibraryScan = ExtractChapterImagesDuringLibraryScan;
		options.RequirePerfectSubtitleMatch = RequirePerfectSubtitleMatch;
		options.SkipSubtitlesIfEmbeddedSubtitlesPresent = SkipSubtitlesIfEmbeddedSubtitlesPresent;
		options.SkipSubtitlesIfAudioTrackMatches = SkipSubtitlesIfAudioTrackMatches;
		options.SaveSubtitlesWithMedia = SaveSubtitlesWithMedia;
		options.SaveLocalMetadata = SaveLocalMetadata;
		options.PreferredMetadataLanguage = PreferredMetadataLanguage?.TwoLetterISOLanguageName;
		options.MetadataCountryCode = MetadataCountryCode?.TwoLetterISORegionName;

		if (FolderCollectionType == Jellyfin.Sdk.Generated.Libraries.AvailableOptions.CollectionType.Tvshows)
		{
			UpdateMetadataFetcher("Series", options, SeriesMetadataFetchers);
			UpdateMetadataFetcher("Season", options, SeasonMetadataFetchers);
			UpdateMetadataFetcher("Episode", options, EpisodeMetadataFetchers);
			UpdateImageFetcher("Series", options, SeriesImageFetchers);
			UpdateImageFetcher("Season", options, SeasonImageFetchers);
			UpdateImageFetcher("Episode", options, EpisodeImageFetchers);
		}
		else if (FolderCollectionType == Jellyfin.Sdk.Generated.Libraries.AvailableOptions.CollectionType.Movies)
		{
			UpdateMetadataFetcher("Movie", options, MovieMetadataFetchers);
			UpdateImageFetcher("Movie", options, MovieImageFetcher);
		}

		options.SubtitleFetcherOrder = [.. SubtitleFetchers.Select(x => x.Name)];
		options.DisabledSubtitleFetchers = [.. SubtitleFetchers.Where(x => !x.IsSelected).Select(x => x.Name)];
		options.SubtitleDownloadLanguages = [.. SubtitleLanguages.Select(x => x.ThreeLetterISOLanguageName)];

		await jellyfinClient.SaveLibraryOptions(Guid.Parse(_info.ItemId), options);
	}

	[RelayCommand]
	private async Task Reset()
	{
		if (_info is null)
		{
			return;
		}

		await Initialize(_info);
	}

	[RelayCommand]
	private void RemoveFolder(string folder) => Locations.Remove(folder);

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

	private static void PopulateMetadataFetcher(string type, LibraryOptionsResultDto options, ObservableCollection<MetadataFetcher> target)
	{
		target.Clear();
		var typeOption = options.TypeOptions?.FirstOrDefault(x => x.Type == type);
		if (typeOption is not null)
		{
			foreach (var (index, item) in (typeOption.MetadataFetchers ?? []).Index())
			{
				target.Add(new MetadataFetcher(item.Name ?? "", target)
				{
					IsSelected = item.DefaultEnabled ?? false
				});
			}

			target[0].CanMoveUp = false;
			target[^1].CanMoveDown = false;
		}
	}

	private static void UpdateMetadataFetcher(string type, LibraryOptions options, IList<MetadataFetcher> fetcher)
	{
		var typeOption = options.TypeOptions?.FirstOrDefault(x => x.Type == type);
		if (typeOption is null)
		{
			return;
		}

		typeOption.MetadataFetcherOrder = [.. fetcher.Select(x => x.Name)];
		typeOption.MetadataFetchers = [.. fetcher.Where(x => x.IsSelected).Select(x => x.Name)];
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

	private static void PopulateImageFetcher(string type, LibraryOptionsResultDto options, ObservableCollection<MetadataFetcher> target)
	{
		target.Clear();
		var typeOption = options.TypeOptions?.FirstOrDefault(x => x.Type == type);
		if (typeOption is not null)
		{
			foreach (var (index, item) in (typeOption.ImageFetchers ?? []).Index())
			{
				target.Add(new MetadataFetcher(item.Name ?? "", target)
				{
					IsSelected = item.DefaultEnabled ?? false
				});
			}

			target[0].CanMoveUp = false;
			target[^1].CanMoveDown = false;
		}
	}

	private static void UpdateImageFetcher(string type, LibraryOptions options, IList<MetadataFetcher> fetcher)
	{
		var typeOption = options.TypeOptions?.FirstOrDefault(x => x.Type == type);
		if (typeOption is null)
		{
			return;
		}

		typeOption.ImageFetcherOrder = [.. fetcher.Select(x => x.Name)];
		typeOption.ImageFetchers = [.. fetcher.Where(x => x.IsSelected).Select(x => x.Name)];
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
