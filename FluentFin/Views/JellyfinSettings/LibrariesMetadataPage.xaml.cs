using FluentFin.Core.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace FluentFin.Views.JellyfinSettings;

public sealed partial class LibrariesMetadataPage : Page
{
	public LibrariesMetadataViewModel ViewModel { get; } = App.GetService<LibrariesMetadataViewModel>();

	public LibrariesMetadataPage()
	{
		InitializeComponent();
	}
}
