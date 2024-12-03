using CommunityToolkit.Mvvm.ComponentModel;
using DynamicData.Binding;
using FluentFin.Core.Contracts.Services;
using System.ComponentModel;
using System.Reactive.Linq;

namespace FluentFin.Core.Settings;

public interface ISettings
{
	ServerSettings ServerSettings { get; }
}

public partial class Settings : ObservableObject, ISettings
{
	private readonly ILocalSettingsService _localSetitngsSerivce;

	[ObservableProperty]
	public partial ServerSettings ServerSettings { get; set; }

	public Settings(ILocalSettingsService localSetitngsSerivce)
	{
		_localSetitngsSerivce = localSetitngsSerivce;
		
		ServerSettings = localSetitngsSerivce.ReadSetting(SettingKeys.ServerSettings)!;

		ObserveObject(ServerSettings, SettingKeys.ServerSettings);
	}

	private void ObserveObject<T>(T target, Key<T> key)
	where T : INotifyPropertyChanged
	{
		target.WhenAnyPropertyChanged()
			  .Throttle(TimeSpan.FromMilliseconds(500))
			  .Subscribe(propInfo => _localSetitngsSerivce.SaveSetting(key, target));
	}
}

