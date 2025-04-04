using FluentFin.Core.Settings;
using FluentFin.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace FluentFin.Views;

public sealed partial class LoginPage
{
	public LoginViewModel ViewModel { get; }

	public LoginPage()
	{
		ViewModel = App.GetService<LoginViewModel>();
		InitializeComponent();
		Loaded += LoginView_Loaded;
	}

	private void LoginView_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
	{
		PasswordBox.Password = ViewModel.Password;
	}

	private void PasswordBox_PasswordChanged(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
	{
		ViewModel.Password = ((PasswordBox)sender).Password;
	}

	private void InputFieldKeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
	{
		if (e.Key == Windows.System.VirtualKey.Enter)
		{
			ViewModel.LoginCommand.Execute(null);
		}
	}

	private void AutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
	{
		if (args.SelectedItem is not SavedUser user)
		{
			return;
		}

		PasswordBox.Password = ViewModel.Unprotect(user.Password);
	}
}
