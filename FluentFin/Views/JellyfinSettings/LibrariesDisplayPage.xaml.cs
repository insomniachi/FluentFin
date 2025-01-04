using FluentFin.Core.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace FluentFin.Views.JellyfinSettings;

public sealed partial class LibrariesDisplayPage : Page
{
	public LibrariesDisplayViewModel ViewModel { get; } = App.GetService<LibrariesDisplayViewModel>();

	public LibrariesDisplayPage()
	{
		InitializeComponent();
	}
}
