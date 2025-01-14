using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentFin.Contracts.ViewModels;
using FluentFin.Core.Contracts.Services;
using Jellyfin.Sdk.Generated.Models;

namespace FluentFin.Core.ViewModels;

public partial class LibrariesMetadataViewModel(IJellyfinClient jellyfinClient) : ObservableObject, INavigationAware
{
	private ServerConfiguration? _configuration;


	[ObservableProperty]
	public partial List<CultureDto> Cultures { get; set; } = [];

	[ObservableProperty]
	public partial List<CountryInfo> Countries { get; set; } = [];

	[ObservableProperty]
	public partial CultureDto? SelectedCulture { get; set; }

	[ObservableProperty]
	public partial CountryInfo? SelectedCountry { get; set; }

	[ObservableProperty]
	public partial double Interval { get; set; }

	[ObservableProperty]
	public partial ServerConfiguration_ChapterImageResolution? Resolution { get; set; }


	public Task OnNavigatedFrom() => Task.CompletedTask;

	public async Task OnNavigatedTo(object parameter)
	{
		Cultures = await jellyfinClient.GetCultures();
		Countries = await jellyfinClient.GetCountries();

		_configuration = await jellyfinClient.GetConfiguration();

		if (_configuration is null)
		{
			return;
		}

		SelectedCulture = Cultures.FirstOrDefault(c => c.TwoLetterISOLanguageName == _configuration.PreferredMetadataLanguage);
		SelectedCountry = Countries.FirstOrDefault(x => x.TwoLetterISORegionName == _configuration.MetadataCountryCode);
		Interval = _configuration.DummyChapterDuration ?? 0;
		Resolution = _configuration.ChapterImageResolution;
	}

	[RelayCommand]
	private async Task Save()
	{
		if (_configuration is null)
		{
			return;
		}

		_configuration.PreferredMetadataLanguage = SelectedCulture?.TwoLetterISOLanguageName;
		_configuration.MetadataCountryCode = SelectedCountry?.TwoLetterISORegionName;
		_configuration.DummyChapterDuration = (int?)Interval;
		_configuration.ChapterImageResolution = Resolution;

		await jellyfinClient.SaveConfiguration(_configuration);
	}

	[RelayCommand]
	private async Task Reset() => await OnNavigatedTo(new());
}
