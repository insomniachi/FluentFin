﻿using FluentFin.Core.Services;
using FluentFin.UI.Core.Contracts.Services;

namespace FluentFin.UI.Core
{
	public interface IPluginManager
	{
		void AddNavigationItems(INavigationViewServiceCore navigationViewService);
		void ConfigurePages(IPageRegistration pageRegistration);
	}
}