using FluentFin.Core.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace FluentFin.Views;

public sealed partial class SeasonPage : Page
{
	public SeasonViewModel ViewModel { get; } = App.GetService<SeasonViewModel>();

    public SeasonPage()
    {
        InitializeComponent();
    }
}
