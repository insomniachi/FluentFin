using FluentFin.Core.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace FluentFin.Views;

public sealed partial class UserParentalControlEditorPage : Page
{
	public UserParentalControlEditorViewModel ViewModel { get; } = App.GetService<UserParentalControlEditorViewModel>();

	public UserParentalControlEditorPage()
	{
		InitializeComponent();
	}
}
