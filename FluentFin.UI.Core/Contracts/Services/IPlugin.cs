using System;
using FluentFin.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FluentFin.UI.Core.Contracts.Services
{
	public interface IPlugin
	{
		void ConfigureServices(IServiceCollection services);
		void ConfigurePages(IPageRegistration pageRegistration);
		void AddNavigationViewItem(INavigationViewServiceCore navigationViewService);
	}
}
