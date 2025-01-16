using FluentFin.Dialogs.ViewModels;
using ReactiveUI;

namespace FluentFin.Dialogs.Views;

public sealed partial class StringPickerDialog : IViewFor<StringPickerViewModel>
{
	public StringPickerViewModel? ViewModel { get; set; } = App.GetService<StringPickerViewModel>();

	object? IViewFor.ViewModel { get => ViewModel; set => ViewModel = (StringPickerViewModel?)value; }

	public StringPickerDialog()
	{
		InitializeComponent();
	}
}
