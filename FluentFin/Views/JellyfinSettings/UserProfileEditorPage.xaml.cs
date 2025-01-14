using FluentFin.Core.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace FluentFin.Views;

public sealed partial class UserProfileEditorPage : Page
{
	public UserProfileEditorViewModel ViewModel { get; } = App.GetService<UserProfileEditorViewModel>();

	public UserProfileEditorPage()
	{
		InitializeComponent();
	}
}
