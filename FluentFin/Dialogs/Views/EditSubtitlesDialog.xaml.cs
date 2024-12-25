using FluentFin.Dialogs.ViewModels;
using ReactiveUI;

namespace FluentFin.Dialogs.Views;

public sealed partial class EditSubtitlesDialog : IViewFor<EditSubtitlesViewModel>
{
	public EditSubtitlesViewModel? ViewModel { get; set; } = App.GetService<EditSubtitlesViewModel>();

	object? IViewFor.ViewModel { get => ViewModel; set => ViewModel = (EditSubtitlesViewModel?)value; }

	public EditSubtitlesDialog()
	{
		InitializeComponent();
	}
}
