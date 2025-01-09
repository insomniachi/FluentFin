using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentFin.Core.Settings;
using System.Collections.ObjectModel;

namespace FluentFin.Core.ViewModels;

public partial class SettingsViewModel(ISettings settings) : ObservableObject
{
	public ObservableCollection<SavedServer> Servers { get; } = settings.Servers;


	[RelayCommand]
	private void DeleteServer(SavedServer server) => Servers.Remove(server);
}
