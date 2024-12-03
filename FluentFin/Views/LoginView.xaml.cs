using FluentFin.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace FluentFin.Views;

public sealed partial class LoginView : UserControl
{
	public LoginViewModel ViewModel { get; }

	public LoginView()
	{
		ViewModel = App.GetService<LoginViewModel>();
		InitializeComponent();
	}

	private void PasswordBox_PasswordChanged(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
	{
		ViewModel.Password = ((PasswordBox)sender).Password;
	}
}
