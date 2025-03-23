using FluentFin.Dialogs.ViewModels;
using ReactiveUI;

namespace FluentFin.Dialogs.Views;

public sealed partial class SyncPlayGroupPickerDialog : IViewFor<SyncPlayGroupPickerViewModel>
{
	public SyncPlayGroupPickerViewModel? ViewModel { get; set; } = App.GetService<SyncPlayGroupPickerViewModel>();

	object? IViewFor.ViewModel { get => ViewModel; set => ViewModel = (SyncPlayGroupPickerViewModel?)value; }

	public SyncPlayGroupPickerDialog()
	{
		InitializeComponent();
	}
}
