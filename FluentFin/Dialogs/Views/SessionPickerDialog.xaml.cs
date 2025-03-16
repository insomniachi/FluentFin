using FluentFin.Dialogs.ViewModels;
using ReactiveUI;

namespace FluentFin.Dialogs.Views;

public sealed partial class SessionPickerDialog : IViewFor<SessionPickerViewModel>
{
	public SessionPickerViewModel? ViewModel { get; set; } = App.GetService<SessionPickerViewModel>();

	object? IViewFor.ViewModel { get => ViewModel; set => ViewModel = (SessionPickerViewModel?)value; }

	public SessionPickerDialog()
	{
		InitializeComponent();
	}
}
