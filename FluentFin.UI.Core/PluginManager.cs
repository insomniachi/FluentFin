using System.Collections.Generic;
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
	}
}
