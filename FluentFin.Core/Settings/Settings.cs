using CommunityToolkit.Mvvm.ComponentModel;
using DynamicData.Binding;
using FluentFin.Core.Contracts.Services;
using System.ComponentModel;
using System.Reactive.Linq;

namespace FluentFin.Core.Settings;

public interface ISettings
{
	ServerSettings ServerSettings { get; }
	List<SavedServer> Servers { get; }
	List<SavedUser> Users { get; }
}

public partial class Settings : ObservableObject, ISettings
{
	private readonly ILocalSettingsService _localSettingsService;

	[ObservableProperty]
	public partial ServerSettings ServerSettings { get; set; }

	public List<SavedServer> Servers { get; set; } = [];
	public List<SavedUser> Users { get; set; } = [];

	public Settings(ILocalSettingsService localSettingsService)
	{
		_localSettingsService = localSettingsService;
		
		ServerSettings = localSettingsService.ReadSetting(SettingKeys.ServerSettings)!;
		Servers = localSettingsService.ReadSetting(SettingKeys.Servers);
		Users = localSettingsService.ReadSetting(SettingKeys.Users);
	}

	//private void ObserveObject<T>(T target, Key<T> key)
	//	where T : INotifyPropertyChanged
	//{
	//	target.WhenAnyPropertyChanged()
	//		  .Throttle(TimeSpan.FromMilliseconds(500))
	//		  .Subscribe(propInfo => _localSettingsService.SaveSetting(key, target));
	//}
}

