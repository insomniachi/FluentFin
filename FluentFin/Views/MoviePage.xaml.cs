using FluentFin.Core.ViewModels;
using Jellyfin.Sdk.Generated.Models;
using Microsoft.UI.Xaml.Controls;


namespace FluentFin.Views;

public sealed partial class MoviePage : Page
{
	public MovieViewModel ViewModel { get; } = App.GetService<MovieViewModel>();

	public MoviePage()
	{
		InitializeComponent();
	}

	public static IEnumerable<BaseItemPerson> GetDirectors(List<BaseItemPerson>? people) => people?.Where(x => x.Type == BaseItemPerson_Type.Director) ?? [];
	public static IEnumerable<BaseItemPerson> GetWriters(List<BaseItemPerson>? people) => people?.Where(x => x.Type == BaseItemPerson_Type.Writer) ?? [];
}
