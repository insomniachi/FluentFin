using FluentFin.Activation;
using FluentFin.Contracts.Services;
using FluentFin.Views;

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace FluentFin.Services;

public class ActivationService : IActivationService
{
	private readonly ActivationHandler<LaunchActivatedEventArgs> _defaultHandler;
	private readonly IEnumerable<IActivationHandler> _activationHandlers;
	private readonly Guid _placementGuid = Guid.Parse("245b5fc3-a858-4106-8dc9-27de8e60e279");
	private UIElement? _shell = null;

	public ActivationService(ActivationHandler<LaunchActivatedEventArgs> defaultHandler, IEnumerable<IActivationHandler> activationHandlers)
	{
		_defaultHandler = defaultHandler;
		_activationHandlers = activationHandlers;
	}

	public async Task ActivateAsync(object activationArgs)
	{
		// Execute tasks before activation.
		await InitializeAsync();

		// Set the MainWindow Content.
		if (App.MainWindow.Content == null)
		{
			_shell = App.GetService<ShellPage>();
			App.MainWindow.Content = _shell ?? new Frame();
		}

		// Handle activation via ActivationHandlers.
		await HandleActivationAsync(activationArgs);

		// Activate the MainWindow.
		App.MainWindow.Maximize();
		//App.MainWindow.AppWindow.EnablePlacementPersistence(_placementGuid, true, App.MainWindow.AppWindow.Id, Microsoft.UI.Windowing.PlacementPersistenceBehaviorFlags.OpenOverLastOpenedWindow | Microsoft.UI.Windowing.PlacementPersistenceBehaviorFlags.AllowLaunchIntoMaximized);
		App.MainWindow.Activate();

		// Execute tasks after activation.
		await DeviceProfileFactory.Initialize();
		await StartupAsync();
	}

	private async Task HandleActivationAsync(object activationArgs)
	{
		var activationHandler = _activationHandlers.FirstOrDefault(h => h.CanHandle(activationArgs));

		if (activationHandler != null)
		{
			await activationHandler.HandleAsync(activationArgs);
		}

		if (_defaultHandler.CanHandle(activationArgs))
		{
			await _defaultHandler.HandleAsync(activationArgs);
		}
	}

	private async Task InitializeAsync()
	{
		await Task.CompletedTask;
	}

	private async Task StartupAsync()
	{
		await Task.CompletedTask;
	}
}
