using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentFin.Contracts.ViewModels;
using FluentFin.Core.Contracts.Services;
using Jellyfin.Sdk.Generated.Models;

namespace FluentFin.Core.ViewModels;

public partial class LibrariesNfoSettingsViewModel(IJellyfinClient jellyfinClient) : ObservableObject, INavigationAware
{
	private XbmcMetadata? _options;

	[ObservableProperty]
	public partial List<UserDto> Users { get; set; }

	[ObservableProperty]
	public partial string? ReleaseDateFormat { get; set; }

	[ObservableProperty]
	public partial bool SaveImagePathsInNfo { get; set; }

	[ObservableProperty]
	public partial bool EnablePathSubstitution { get; set; }

	[ObservableProperty]
	public partial bool EnableExtraThumbsDuplication { get; set; }

	[ObservableProperty]
	public partial UserDto? SelectedUser { get; set; }

	public Task OnNavigatedFrom() => Task.CompletedTask;

	public async Task OnNavigatedTo(object parameter)
	{
		Users = [new UserDto { Name = "None" }, .. await jellyfinClient.GetUsers()];

		_options = await jellyfinClient.GetXbmcMetadata();

		if (_options is null)
		{
			return;
		}

		ReleaseDateFormat = _options.ReleaseDateFormat;
		EnablePathSubstitution = _options.EnablePathSubstitution;
		SaveImagePathsInNfo = _options.SaveImagePathsInNfo;
		EnableExtraThumbsDuplication = _options.EnableExtraThumbsDuplication;
		SelectedUser = Users.FirstOrDefault(x => x.Id == _options.UserId);
	}

	[RelayCommand]
	private async Task Save()
	{
		if (_options is null)
		{
			return;
		}

		_options.ReleaseDateFormat = ReleaseDateFormat;
		_options.EnablePathSubstitution = EnablePathSubstitution;
		_options.SaveImagePathsInNfo = SaveImagePathsInNfo;
		_options.EnableExtraThumbsDuplication = EnableExtraThumbsDuplication;
		_options.UserId = SelectedUser?.Id;

		await jellyfinClient.SaveXbmcMetadata(_options);
	}

	[RelayCommand]
	private async Task Reset() => await OnNavigatedTo(new());
}
