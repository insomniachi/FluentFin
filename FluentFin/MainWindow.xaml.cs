using FluentFin.Contracts.Services;
using FluentFin.Core.ViewModels;
using FluentFin.ViewModels;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace FluentFin;

public sealed partial class MainWindow : WindowEx
{
	public IMainWindowViewModel ViewModel { get; } = App.GetService<IMainWindowViewModel>();

    public MainWindow()
    {
        InitializeComponent();
        ExtendsContentIntoTitleBar = true;
        AppWindow.SetIcon("Assets/jellyfin.ico");
	}


	private void TitleBar_PaneToggleRequested(Microsoft.UI.Xaml.Controls.TitleBar sender, object args)
    {
        Shell.NavigationViewControl.IsPaneOpen ^= true;
    }

    private void TitleBar_BackRequested(Microsoft.UI.Xaml.Controls.TitleBar sender, object args)
    {
        if (!Shell.NavigationFrame.CanGoBack)
        {
            return;
        }

        Shell.NavigationFrame.GoBack();
    }
}
