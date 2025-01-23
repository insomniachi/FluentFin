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

	private void TitleBar_BackRequested(Microsoft.UI.Xaml.Controls.TitleBar sender, object args)
	{
		ViewModel.GoBack();
	}

	private void CloseTitleBarFooterFlyout(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
	{
		FooterFlyout.Hide();
	}
}
