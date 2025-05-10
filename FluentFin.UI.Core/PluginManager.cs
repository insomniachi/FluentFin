using System.Collections.Generic;
using System.Linq;
using FluentFin.Core.Contracts.Services;
using FluentFin.Core.Services;
using FluentFin.UI.Core.Contracts.Services;

namespace FluentFin.UI.Core
{
	public class PluginManager(IEnumerable<IPlugin> plugins) : IPluginManager
	{
		private readonly List<IPlugin> _plugins = [.. plugins];

		public void AddNavigationItems(INavigationViewServiceCore navigationViewService)
		{
			foreach (var plugin in _plugins)
			{
				plugin.AddNavigationViewItem(navigationViewService);
			}
		}

		public void ConfigurePages(IPageRegistration pageRegistration)
		{
			foreach (var plugin in _plugins)
			{
				plugin.ConfigurePages(pageRegistration);
			}
		}

		public void LoadOptions(ILocalSettingsService localSettingsService)
		{
			foreach (var plugin in _plugins.OfType<IPluginWithOptions>())
			{
				plugin.LoadOptions(localSettingsService);
			}
		}
	}
}
