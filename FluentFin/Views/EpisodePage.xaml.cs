using FluentFin.Core.ViewModels;
using Jellyfin.Sdk.Generated.Models;
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
