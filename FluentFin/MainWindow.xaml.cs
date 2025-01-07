using FluentFin.Contracts.Services;
using FluentFin.Core;
using FluentFin.Core.ViewModels;
using FluentFin.Helpers;
using FluentFin.Views;
using ReactiveUI;

namespace FluentFin;

public sealed partial class MainWindow : WindowEx
{
	public IMainWindowViewModel ViewModel { get; } = App.GetService<IMainWindowViewModel>();

    public MainWindow()
    {
        InitializeComponent();
        ExtendsContentIntoTitleBar = true;
        AppWindow.SetIcon("Assets/jellyfin.ico");

        App.GetKeyedService<INavigationService>(NavigationRegions.InitialSetup).Frame = RootFrame;
	}


	private void TitleBar_PaneToggleRequested(Microsoft.UI.Xaml.Controls.TitleBar sender, object args)
    {
        if(RootFrame.Content is not ShellPage shell)
        {
            return;
        }

        shell.NavigationViewControl.IsPaneOpen ^= true;
    }

    private void TitleBar_BackRequested(Microsoft.UI.Xaml.Controls.TitleBar sender, object args)
    {
		if (RootFrame.Content is not ShellPage shell)
		{
			return;
		}

		if (!shell.NavigationFrame.CanGoBack)
        {
            return;
        }

        shell.NavigationFrame.GoBack();
    }
}
