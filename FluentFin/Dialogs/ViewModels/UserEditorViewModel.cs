using CommunityToolkit.Mvvm.ComponentModel;
using FluentFin.Contracts.Services;
using FluentFin.Contracts.ViewModels;
using FluentFin.Core.ViewModels;
using Jellyfin.Sdk.Generated.Models;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;

namespace FluentFin.Dialogs.ViewModels;

public partial class UserEditorViewModel([FromKeyedServices("UserEditor")]INavigationServiceCore navigationService) : ObservableObject, INavigationAware
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

		//this.WhenAnyValue(x => x.Section)
		//	.Subscribe(section => navigationService.NavigateTo("", User));

		return Task.CompletedTask;
	}
}
