using FluentFin.Core.ViewModels;
using Microsoft.UI.Xaml.Controls;


namespace FluentFin.Views;

public sealed partial class EpisodePage : Page
{
	public EpisodeViewModel ViewModel { get; } = App.GetService<EpisodeViewModel>();

	public EpisodePage()
	{
		InitializeComponent();
	}
}
