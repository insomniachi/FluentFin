using FluentFin.Dialogs.ViewModels;
using ReactiveUI;

namespace FluentFin.Dialogs.Views;

public sealed partial class MediaInfoDialog : IViewFor<MediaInfoViewModel>
{
	public MediaInfoViewModel? ViewModel { get; set; } = App.GetService<MediaInfoViewModel>();

	object? IViewFor.ViewModel { get => ViewModel; set => ViewModel = (MediaInfoViewModel?)value; }

	public MediaInfoDialog()
	{
		InitializeComponent();
	}
}
