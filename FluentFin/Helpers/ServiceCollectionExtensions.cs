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
}
