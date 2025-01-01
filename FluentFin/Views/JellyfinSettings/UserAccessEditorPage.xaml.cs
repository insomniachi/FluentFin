using FluentFin.Core.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace FluentFin.Views;

public sealed partial class UserAccessEditorPage : Page
{
	public UserAccessEditorViewModel ViewModel { get; } = App.GetService<UserAccessEditorViewModel>();
    
    public UserAccessEditorPage()
    {
        InitializeComponent();
    }
}
