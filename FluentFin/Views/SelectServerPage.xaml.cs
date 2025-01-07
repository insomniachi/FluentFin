using FluentFin.Core.Settings;
using FluentFin.Core.ViewModels;
using FluentFin.Helpers;
using Microsoft.UI.Xaml.Controls;

namespace FluentFin.Views;

public sealed partial class SelectServerPage : Page
{
	public SelectServerViewModel ViewModel { get; } = App.GetService<SelectServerViewModel>();

	public SelectServerPage()
	{
		InitializeComponent();
	}

	private void InputFieldKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
	{
		if (e.Key == Windows.System.VirtualKey.Enter)
		{
			//ViewModel.LoginCommand.Execute(null);
		}
	}

	private async void ServerSelected(ItemsView sender, ItemsViewItemInvokedEventArgs args)
	{
		if(args.InvokedItem is not SavedServer server)
		{
			return;
		}

		ViewModel.CheckConnectivityAndGoToLoginCommand.Execute(server.GetServerUrl());
	}

}
