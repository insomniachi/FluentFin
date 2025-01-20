using FluentFin.Dialogs.ViewModels;
using ReactiveUI;

namespace FluentFin.Dialogs.Views;

public sealed partial class QuickConnectDialog : IViewFor<QuickConnectViewModel>
{
	public QuickConnectViewModel? ViewModel { get; set; } = App.GetService<QuickConnectViewModel>();

	object? IViewFor.ViewModel { get => ViewModel; set => ViewModel = (QuickConnectViewModel?)value; }

	public QuickConnectDialog()
	{
		InitializeComponent();
	}
}
