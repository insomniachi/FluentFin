using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentFin.Contracts.ViewModels;
using FluentFin.Core.Contracts.Services;
using Jellyfin.Sdk.Generated.Models;

namespace FluentFin.Core.ViewModels
{
	public partial class PlaybackResumeViewModel(IJellyfinClient jellyfinClient) : ObservableObject, INavigationAware
	{
		private ServerConfiguration? _serverConfiguration;

		[ObservableProperty]
		public partial double MinimumResumePercentage { get; set; }

		[ObservableProperty]
		public partial double MaximumResumePercentage { get; set; }

		[ObservableProperty]
		public partial double MinimumResumeDuration { get; set; }

		[ObservableProperty]
		public partial double InternetStreamingBitRateLimit { get; set; }

		public Task OnNavigatedFrom() => Task.CompletedTask;

		public async Task OnNavigatedTo(object parameter)
		{
			_serverConfiguration = await jellyfinClient.GetConfiguration();

			if(_serverConfiguration is null)
			{
				return;
			}

			MinimumResumePercentage = _serverConfiguration.MinResumePct ?? 0;
			MaximumResumePercentage = _serverConfiguration.MaxResumePct ?? 0;
			MinimumResumeDuration = _serverConfiguration.MinResumeDurationSeconds ?? 0;
			InternetStreamingBitRateLimit = _serverConfiguration.RemoteClientBitrateLimit ?? 0;
		}

		[RelayCommand]
		private async Task Save()
		{
			if (_serverConfiguration is null)
			{
				return;
			}

			_serverConfiguration.MinResumePct = (int?)MinimumResumePercentage;
			_serverConfiguration.MaxResumePct = (int?)MaximumResumePercentage;
			_serverConfiguration.MinResumeDurationSeconds = (int?)MinimumResumeDuration;
			_serverConfiguration.RemoteClientBitrateLimit = (int?)InternetStreamingBitRateLimit;

			await jellyfinClient.SaveConfiguration(_serverConfiguration);
		}

		[RelayCommand]
		private async Task Reset() => await OnNavigatedTo(new());
	}
}
