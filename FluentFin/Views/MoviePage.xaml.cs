using FluentFin.Core.ViewModels;
using Microsoft.UI.Xaml.Controls;


namespace FluentFin.Views;

public sealed partial class MoviePage : Page
{
	public MovieViewModel ViewModel { get; } = App.GetService<MovieViewModel>();

	public MoviePage()
	{
		InitializeComponent();
	}
}
