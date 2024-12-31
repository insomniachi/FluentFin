using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentFin.Contracts.Services;
using FluentFin.Contracts.ViewModels;
using FluentFin.Core.Contracts.Services;
using Jellyfin.Sdk.Generated.Models;
using Microsoft.Extensions.DependencyInjection;

namespace FluentFin.Core.ViewModels;

public partial class UsersViewModel(IJellyfinClient jellyfinClient,
									[FromKeyedServices("Settings")]INavigationServiceCore navigationService) : ObservableObject, INavigationAware
{
	[ObservableProperty]
	public partial List<UserDto> Users { get; set; } = [];

	public Task OnNavigatedFrom() => Task.CompletedTask;

	public async Task OnNavigatedTo(object parameter)
	{
		Users = await jellyfinClient.GetUsers();
	}

	public void Navigate(UserDto user, UserEditorSection section)
	{
		navigationService.NavigateTo("FluentFin.Dialogs.ViewModels.UserEditorViewModel", new UserEditorViewModelNavigationParameter(user, section));
	}
}

public record UserEditorViewModelNavigationParameter(UserDto User, UserEditorSection Section);

public enum UserEditorSection
{
	Profile,
	Access,
	ParentalControl,
	Password
}