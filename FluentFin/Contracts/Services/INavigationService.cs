using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace FluentFin.Contracts.Services;

public interface INavigationService : INavigationServiceCore
{
	event NavigatedEventHandler Navigated;

	Frame? Frame { get; set; }
}
