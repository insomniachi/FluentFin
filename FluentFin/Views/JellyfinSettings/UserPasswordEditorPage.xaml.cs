using FluentFin.Core.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace FluentFin.Views;

public sealed partial class UserPasswordEditorPage : Page
{
	public UserPasswordEditorViewModel ViewModel { get; } = App.GetService<UserPasswordEditorViewModel>();

	public UserPasswordEditorPage()
	{
		InitializeComponent();
	}

	private async void ChangePasswordClicked(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
	{
		if (ConfirmPassword.Password != NewPassword.Password)
		{
			var dialog = new ContentDialog
			{
				XamlRoot = App.MainWindow.Content.XamlRoot,
				Title = "",
				Content = $"Password and password confirmation must match",
				CloseButtonText = "Close",
				DefaultButton = ContentDialogButton.Close
			};

			var response = await dialog.ShowAsync();

			return;
		}

		await ViewModel.ChangePassword(CurrentPassword.Password, NewPassword.Password);
	}

	private async void ResetPasswordClicked(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
	{
		await ViewModel.ResetPassword();
	}
}
