using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentFin.Contracts.ViewModels;
using FluentFin.Core.Contracts.Services;
using Jellyfin.Sdk.Generated.Models;

namespace FluentFin.Core.ViewModels
{
	public abstract partial class ServerConfigurationViewModel(IJellyfinClient jellyfinClient) : ObservableObject, INavigationAware
	{
		protected readonly IJellyfinClient _jellyfinClient = jellyfinClient;
		private ServerConfiguration? _configuration;

		[ObservableProperty]
		public partial List<JellyfinConfigItemViewModel> Items { get; set; } = [];

		public Task OnNavigatedFrom() => Task.CompletedTask;

		public async Task OnNavigatedTo(object parameter)
		{
			_configuration = await _jellyfinClient.GetConfiguration();

			if (_configuration is null)
			{
				return;
			}

			Items = CreateItems(_configuration);
		}

		protected abstract List<JellyfinConfigItemViewModel> CreateItems(ServerConfiguration config);

		[RelayCommand]
		private async Task Save()
		{
			if (_configuration is null)
			{
				return;
			}

			foreach (var item in Items)
			{
				item.Save();
			}

			await _jellyfinClient.SaveConfiguration(_configuration);
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
}
