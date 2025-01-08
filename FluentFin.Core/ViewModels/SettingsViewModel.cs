using CommunityToolkit.Mvvm.ComponentModel;
using FluentFin.Core.Settings;
using System.Collections.ObjectModel;

namespace FluentFin.Core.ViewModels;

public partial class SettingsViewModel(ISettings settings) : ObservableObject
{
	public ObservableCollection<SavedServer> Servers { get; } = settings.Servers;
}
