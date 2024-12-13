using FluentFin.Core.ViewModels;
using FluentFin.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace FluentFin.Views;

public sealed partial class TitleBarControl : UserControl
{
	public TitleBarViewModel ViewModel { get; } = (TitleBarViewModel)App.GetService<ITitleBarViewModel>();

	public TitleBarControl()
	{
		InitializeComponent();
	}

	private void TitleBar_PaneToggleRequested(Microsoft.UI.Xaml.Controls.TitleBar sender, object args)
	{
		if(DataContext is not ITitleBarViewModel vm)
		{
			return;
		}

		vm.TogglePane();
        }

	private void TitleBar_BackRequested(Microsoft.UI.Xaml.Controls.TitleBar sender, object args)
	{
		if (DataContext is not ITitleBarViewModel vm)
		{
			return;
		}

		vm.GoBack();
	}
    }
