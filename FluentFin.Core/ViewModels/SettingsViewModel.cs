using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentFin.Contracts.ViewModels;
using FluentFin.Core.Contracts.Services;
using FluentFin.Core.Settings;

namespace FluentFin.Core.ViewModels;

public partial class SettingsViewModel(ISettings settings) : ObservableObject, INavigationAware
{
	public ObservableCollection<SavedServer> Servers { get; } = settings.Servers;

	public MediaPlayerType MediaPlayerType
	{
		get => field;
		set
		{
			if (SetProperty(ref field, value))
			{
				settings.MediaPlayer = value;
			}
		}
	}

	public Task OnNavigatedFrom() => Task.CompletedTask;

	public Task OnNavigatedTo(object parameter)
	{
		MediaPlayerType = settings.MediaPlayer;

		return Task.CompletedTask;
	}

	[RelayCommand]
	private void DeleteServer(SavedServer server) => Servers.Remove(server);
}
