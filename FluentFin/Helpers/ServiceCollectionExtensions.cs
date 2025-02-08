using FluentFin.Contracts.Services;
using FluentFin.Core.Contracts.Services;
using FluentFin.Core.Services;
using FluentFin.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using ReactiveUI;
using System.ComponentModel;

namespace FluentFin.Helpers;

internal static class ServiceCollectionExtensions
{
	public static IServiceCollection AddDialog<TViewModel, TView>(this IServiceCollection services)
		where TViewModel : class, INotifyPropertyChanged
		where TView : ContentDialog, IViewFor<TViewModel>
	{
		services.AddTransient<TViewModel>();
		services.AddTransient<IViewFor<TViewModel>, TView>();

		return services;
	}

	public static IServiceCollection AddFrameNavigation(this IServiceCollection services, string key)
	{
		services.AddKeyedSingleton<INavigationService, NavigationService>(key);
		services.AddKeyedSingleton<INavigationServiceCore>(key, (sp, key) => sp.GetRequiredKeyedService<INavigationService>(key));

		return services;
	}

	public static IServiceCollection AddNavigationViewNavigation(this IServiceCollection services, string key)
	{
		services.AddFrameNavigation(key);
		services.AddKeyedSingleton<INavigationViewService, NavigationViewService>(key, (sp, key) =>
		{
			return new NavigationViewService(sp.GetRequiredKeyedService<INavigationService>(key),
											 sp.GetRequiredService<IPageService>());
		});
		return services;
	}
}
