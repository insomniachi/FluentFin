using System;
using FluentFin.Core.Contracts.Services;
using FluentFin.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FluentFin.UI.Core.Contracts.Services
{
	public interface IPlugin
	{
		Guid Id { get; }
		void ConfigureServices(IServiceCollection services);
		void ConfigurePages(IPageRegistration pageRegistration);
		void AddNavigationViewItem(INavigationViewServiceCore navigationViewService);
	}

	public interface IPluginWithOptions : IPlugin
	{
		void LoadOptions(ILocalSettingsService localSettingsService);
	}
}
