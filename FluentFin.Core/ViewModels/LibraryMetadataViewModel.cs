using FluentFin.Contracts.ViewModels;
using FluentFin.Core.Contracts.Services;
using Jellyfin.Sdk.Generated.Models;

namespace FluentFin.Core.ViewModels;

public partial class LibrariesMetadataViewModel(IJellyfinClient jellyfinClient) : ServerConfigurationViewModel(jellyfinClient), INavigationAware
{
	public List<CultureDto> Cultures { get; set; } = [];
	public List<CountryInfo> Countries { get; set; } = [];

	protected override List<JellyfinConfigItemViewModel> CreateItems(ServerConfiguration config)
	{
		return
		[
			new JellyfinGroupedConfigItemViewModel()
			{
				DisplayName = "Preferred Metadata Language",
				Description = "These are your defaults and can be customized on a per-library basis.",
				Items =
				[
					new JellyfinSelectableConfigItemViewModel(() => Cultures.FirstOrDefault(c => c.TwoLetterISOLanguageName == config.PreferredMetadataLanguage),
															  value => config.PreferredMetadataLanguage = (value as CultureDto)?.TwoLetterISOLanguageName,
															  Cultures, nameof(CultureDto.Name))
					{
						DisplayName = "Language"
					},
					new JellyfinSelectableConfigItemViewModel(() => Countries.FirstOrDefault(c => c.TwoLetterISORegionName == config.MetadataCountryCode),
															  value => config.MetadataCountryCode = (value as CountryInfo)?.TwoLetterISORegionName,
															  Countries, nameof(CountryInfo.DisplayName))
					{
						DisplayName = "Country/Region"
					}
				]
			},
			new JellyfinGroupedConfigItemViewModel
			{
				DisplayName = "Chapter Images",
				Items =
				[
					new JellyfinConfigItemViewModel<double>(() => config.DummyChapterDuration ?? 0, value => config.DummyChapterDuration = (int)value)
					{
						DisplayName = "Interval",
						Description = "The interval between dummy chapters. Set to 0 to disable dummy chapter generation. Changing this will have no effect on existing dummy chapters."
					},
					new JellyfinSelectableConfigItemViewModel(() => config.ChapterImageResolution,
															  value => config.ChapterImageResolution = (value as ServerConfiguration_ChapterImageResolution?),
															  [.. Enum.GetValues<ServerConfiguration_ChapterImageResolution>()])
					{
						DisplayName = "Resolution",
						Description = "The resolution of the extracted chapter images. Changing this will have no effect on existing dummy chapters."
					}
				]
			}

		];
	}

	async Task INavigationAware.OnNavigatedTo(object parameter)
	{
		Cultures = await _jellyfinClient.GetCultures();
		Countries = await _jellyfinClient.GetCountries();

		await base.OnNavigatedTo(parameter);
	}
}
