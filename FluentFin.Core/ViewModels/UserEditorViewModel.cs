using CommunityToolkit.Mvvm.ComponentModel;
using FluentFin.Contracts.Services;
using FluentFin.Contracts.ViewModels;
using FluentFin.Core;
using FluentFin.Core.ViewModels;
using Jellyfin.Sdk.Generated.Models;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using System.Diagnostics;
using System.Reactive.Linq;

namespace FluentFin.Dialogs.ViewModels;

public partial class UserEditorViewModel([FromKeyedServices(NavigationRegions.UserEditor)] INavigationServiceCore navigationService) : ObservableObject, INavigationAware
{
	[ObservableProperty]
	public partial UserDto? User { get; set; }

	[ObservableProperty]
	public partial UserEditorSection Section { get; set; } = UserEditorSection.Profile;

	public Task OnNavigatedFrom()
	{
		return Task.CompletedTask;
	}

	public Task OnNavigatedTo(object parameter)
	{
		if(parameter is UserEditorViewModelNavigationParameter args)
		{
			User = args.User;
			Section = args.Section;
		}

		this.WhenAnyValue(x => x.Section)
			.Select(ConvertToPageKey)
			.Subscribe(key => navigationService.NavigateTo(key, User));

		return Task.CompletedTask;
	}

	private string ConvertToPageKey(UserEditorSection section)
	{
		return section switch
		{
			UserEditorSection.Profile => typeof(UserProfileEditorViewModel).FullName!,
			UserEditorSection.Access => "Access",
			UserEditorSection.ParentalControl => "ParentalControl",
			UserEditorSection.Password => "Password",
			_ => throw new UnreachableException()
		};
	}
}
