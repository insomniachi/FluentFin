using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentFin.Core.Contracts.Services;
using FluentFin.Core.Services;
using FluentFin.Plugins.Jellyseer.ViewModels;
using FluentFin.Plugins.Jellyseer.Views;
using FluentFin.UI.Core.Contracts.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FluentFin.Plugins.Jellyseer;

public class JellyseerPlugin : IPluginWithOptions
{
	public Guid Id { get; } = Guid.Parse("ba4e6014-505b-4570-b11c-009281d337cf");

	public void AddNavigationViewItem(INavigationViewServiceCore navigationViewService)
	{
		navigationViewService.AddNavigationItem(new Core.Settings.CustomNavigationViewItem
		{
			Key = typeof(JellyseerDashboardViewModel).FullName!,
			Name = "Jellyseer",
			Glyph = "\uE8A7",
		});
	}

	public void ConfigurePages(IPageRegistration pageRegistration)
	{
		pageRegistration.Configure<JellyseerDashboardViewModel, JellyseerDashboardPage>();
	}

	public void ConfigureServices(IServiceCollection services)
	{
		services.AddSingleton<IJellyseerClient, JellyseerClient>();
		services.AddTransient<JellyseerDashboardViewModel>();
	}

	public void LoadOptions(ILocalSettingsService localSettingsService)
	{
		var node = localSettingsService.ReadSettingRaw($"Plugin_{Id}");

		if(node is null)
		{
			JellyseerOptions.Current = new JellyseerOptions();
			JellyseerOptions.Current.Save = () =>
			{
				localSettingsService.SaveSetting($"Plugin_{Id}", JellyseerOptions.Current);
			};
			return;
		}

		var jObject = node.AsObject();
		//var verson = jObject["Version"]!.GetValue<int>();
		JellyseerOptions.Current = jObject.Deserialize<JellyseerOptions>()!;
		JellyseerOptions.Current.Save = () =>
		{
			localSettingsService.SaveSetting($"Plugin_{Id}", JellyseerOptions.Current);
		};
	}
}

public class JellyseerOptions
{
	public int Version { get; set; } = 1;
	public Dictionary<string, string> ServerMapping { get; set; } = new();

	public static JellyseerOptions Current { get; set; } = null!;

	[JsonIgnore]
	public Action? Save { get; set; }
}
