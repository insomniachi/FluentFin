using FluentFin.Dialogs.ViewModels;
using ReactiveUI;
using Windows.Globalization.NumberFormatting;

namespace FluentFin.Dialogs.Views;

public sealed partial class EditMetadataDialog : IViewFor<EditMetadataViewModel>
{
	public EditMetadataViewModel? ViewModel { get; set; } = App.GetService<EditMetadataViewModel>();

	object? IViewFor.ViewModel { get => ViewModel; set => ViewModel = (EditMetadataViewModel?)value; }

	public EditMetadataDialog()
	{
		InitializeComponent();
	}
}
