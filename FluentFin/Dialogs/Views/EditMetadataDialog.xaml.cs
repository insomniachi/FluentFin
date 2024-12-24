using FluentFin.Dialogs.ViewModels;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using ReactiveUI;

namespace FluentFin.Dialogs.Views;

public sealed partial class EditMetadataDialog : IViewFor<EditMetadataViewModel>
{
	public EditMetadataViewModel? ViewModel { get; set; } = App.GetService<EditMetadataViewModel>();

	object? IViewFor.ViewModel { get => ViewModel; set => ViewModel = (EditMetadataViewModel?)value; }

	public EditMetadataDialog()
	{
		InitializeComponent();
	}

	public static ImageSource GetImage() => new BitmapImage();
}
