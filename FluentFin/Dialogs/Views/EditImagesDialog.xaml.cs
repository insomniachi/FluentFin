using FluentFin.Dialogs.ViewModels;
using ReactiveUI;

namespace FluentFin.Dialogs.Views;

public sealed partial class EditImagesDialog : IViewFor<EditImagesViewModel>
{
	public EditImagesViewModel? ViewModel { get; set; } = App.GetService<EditImagesViewModel>();

	object? IViewFor.ViewModel { get => ViewModel; set => ViewModel = (EditImagesViewModel?)value; }

	public EditImagesDialog()
	{
		InitializeComponent();
	}
}
