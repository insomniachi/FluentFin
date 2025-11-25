using FluentFin.Core.ViewModels;
using Microsoft.UI.Xaml.Controls;


namespace FluentFin.Views;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class HomePage : Page
{
	public HomeViewModel ViewModel { get; } = App.GetService<HomeViewModel>();

	public HomePage()
	{
		InitializeComponent();
	}

	private void NextUpInvoked(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
	{
		ViewModel.NextUpCommand.Execute(null);
    }

	private void ContinueInvoked(Microsoft.UI.Xaml.Input.KeyboardAccelerator sender, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs args)
	{
		ViewModel.ContinueCommand.Execute(null);
    }
}
