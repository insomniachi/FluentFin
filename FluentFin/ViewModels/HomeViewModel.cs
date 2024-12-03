using CommunityToolkit.Mvvm.ComponentModel;
using FluentFin.Contracts.ViewModels;
using FluentFin.Core.Contracts.Services;

namespace FluentFin.ViewModels
{
	public partial class HomeViewModel(IJellyfinClient jellyfinClient) : ObservableObject, INavigationAware
	{
		public Task OnNavigatedFrom() => Task.CompletedTask;

		public async Task OnNavigatedTo(object parameter)
		{
			var continueWatching = await jellyfinClient.GetContinueWatching();
		}
	}
}
