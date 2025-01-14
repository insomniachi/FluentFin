using FluentFin.Dialogs.ViewModels;
using Jellyfin.Sdk.Generated.Models;
using ReactiveUI;

namespace FluentFin.Dialogs.Views;

public sealed partial class AddUserDialog : IViewFor<AddUserViewModel>
{
	public AddUserViewModel? ViewModel { get; set; } = App.GetService<AddUserViewModel>();

	object? IViewFor.ViewModel { get => ViewModel; set => ViewModel = (AddUserViewModel?)value; }

	public AddUserDialog()
	{
		InitializeComponent();
	}

	private void PasswordBox_PasswordChanged(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
	{
		if (ViewModel is null)
		{
			return;
		}

		ViewModel.Password = PasswordBox.Password;
	}

	private void ListView_SelectionChanged(object sender, Microsoft.UI.Xaml.Controls.SelectionChangedEventArgs e)
	{
		if (ViewModel is null)
		{
			return;
		}

		if (ListView.SelectedItems is IEnumerable<BaseItemDto> selected)
		{
			ViewModel.SelectedItems = selected.ToList();
		}
	}
}
