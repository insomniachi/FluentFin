using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentFin.Contracts.ViewModels;
using FluentFin.Core.Contracts.Services;
using Jellyfin.Sdk.Generated.Models;

namespace FluentFin.Core.ViewModels;

public partial class LibrariesNfoSettingsViewModel(IJellyfinClient jellyfinClient) : ObservableObject, INavigationAware
{
	private XbmcMetadata? _options;

	public List<UserDto> Users { get; set; } = [];

	[ObservableProperty]
	public partial List<JellyfinConfigItemViewModel> Items { get; set; } = [];

	public Task OnNavigatedFrom() => Task.CompletedTask;

	public async Task OnNavigatedTo(object parameter)
	{
		Users = [new UserDto { Name = "None" }, .. await jellyfinClient.GetUsers()];

		_options = await jellyfinClient.GetXbmcMetadata();

		if (_options is null)
		{
			return;
		}

		Items =
		[
			new JellyfinSelectableConfigItemViewModel(() => Users.FirstOrDefault(x => x.Id == _options.UserId),
													  value => _options.UserId = (value as UserDto)?.Id,
													  Users, nameof(UserDto.Name))
			{
				DisplayName = "Save User watch data to NFO files for",
				Description = "Save watch data to NFO files for other applications to use.",
			},
			new JellyfinConfigItemViewModel<string>(() => _options.ReleaseDateFormat ?? "", value => _options.ReleaseDateFormat = value)
			{
				DisplayName = "Release date format",
				Description = "All dates within NFO files will be parsed using this format."
			},
			new JellyfinConfigItemViewModel<bool>(() => _options.SaveImagePathsInNfo, value => _options.SaveImagePathsInNfo = value)
			{
				DisplayName = "Save image paths within NFO files",
				Description = "This is recommended if you have image file names that don't conform to Kodi guidelines.Enable path substitution of image paths using the server's path substitution settings." +
							  "Enable path substitution of image paths using the server's path substitution settings.Enable path substitution of image paths using the server's path substitution settings."
			},
			new JellyfinConfigItemViewModel<bool>(() => _options.EnablePathSubstitution, value => _options.EnablePathSubstitution = value)
			{
				DisplayName = "Enable path substitution",
				Description = "Enable path substitution of image paths using the server's path substitution settings."
			},
			new JellyfinConfigItemViewModel<bool>(() => _options.EnableExtraThumbsDuplication, value => _options.EnableExtraThumbsDuplication = value)
			{
				DisplayName = "Copy extrafanart to extrathumbs field",
				Description = "When downloading images they can be saved into both extrafanart and extrathumbs for maximum Kodi skin compatibility."
			}
		];
	}

	[RelayCommand]
	private async Task Save()
	{
		if (_options is null)
		{
			return;
		}

		foreach (var item in Items)
		{
			item.Save();
		}

		await jellyfinClient.SaveXbmcMetadata(_options);
	}

	[RelayCommand]
	private void Reset()
	{
		foreach (var item in Items)
		{
			item.Reset();
		}
	}
}
