using FluentFin.ViewModels;
using FlyleafLib;
using FlyleafLib.Controls.WinUI;
using Microsoft.UI.Xaml.Controls;

namespace FluentFin.Views;

public sealed partial class VideoPlayerPage : Page
{
	public VideoPlayerViewModel ViewModel { get; } = App.GetService<VideoPlayerViewModel>();

	public VideoPlayerPage()
	{
		InitializeComponent();

		FSC.FullScreenExit += (o, e) =>
		{
			TransportControls.FullWindowSymbol.Symbol = Symbol.FullScreen;
			App.MainWindow.IsShownInSwitchers = true;
			Task.Run(() => { Thread.Sleep(10); Utils.UIInvoke(() => flyleafHost.KFC?.Focus(Microsoft.UI.Xaml.FocusState.Keyboard)); });
		};

		FSC.FullScreenEnter += (o, e) =>
		{
			TransportControls.FullWindowSymbol.Symbol = Symbol.BackToWindow;
			App.MainWindow.IsShownInSwitchers = false;
			flyleafHost.KFC?.Focus(Microsoft.UI.Xaml.FocusState.Keyboard);
		};

	}
}
