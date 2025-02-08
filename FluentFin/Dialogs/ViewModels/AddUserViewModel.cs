using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentFin.Core;
using FluentFin.Core.Contracts.Services;
using FluentFin.Core.Services;
using FluentFin.Core.ViewModels;
using Jellyfin.Sdk.Generated.Models;
using Microsoft.Extensions.DependencyInjection;

namespace FluentFin.Dialogs.ViewModels
{
	public partial class AddUserViewModel(IJellyfinClient jellyfinClient,
										  [FromKeyedServices(NavigationRegions.Settings)] INavigationServiceCore navigationService) : ObservableObject, IHandleClose
	{
		[ObservableProperty]
		public partial string? Username { get; set; }

		[ObservableProperty]
		public partial string? Password { get; set; }

		[ObservableProperty]
		public partial bool EnableAllFolders { get; set; }

		[ObservableProperty]
		public partial List<BaseItemDto> MediaFolders { get; set; } = [];

		[ObservableProperty]
		public partial List<BaseItemDto> SelectedItems { get; set; } = [];

		public bool CanClose { get; set; }

		public async Task Initialize()
		{
			var response = await jellyfinClient.GetMediaFolders();

			if (response is { Items.Count: > 0 })
			{
				MediaFolders = response.Items;
			}
		}


		[RelayCommand]
		private async Task AddUser()
		{
			if (string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
			{
				return;
			}

			var user = await jellyfinClient.CreateUser(Username, Password);

			if (user is null)
			{
				return;
			}

			if (user.Policy is { } policy)
			{
				if (EnableAllFolders)
				{
					policy.EnableAllFolders = true;
				}
				else
				{
					policy.EnableAllFolders = false;
					policy.EnabledFolders = SelectedItems.Select(x => x.Id).ToList();
				}

				await jellyfinClient.UpdatePolicy(user, policy);
			}

			navigationService.NavigateTo<UserEditorViewModel>(new UserEditorViewModelNavigationParameter(user, UserEditorSection.Profile));
		}
	}
}
