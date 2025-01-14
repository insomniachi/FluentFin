using FluentFin.Dialogs.ViewModels;
using Jellyfin.Sdk.Generated.Models;
using ReactiveUI;

namespace FluentFin.Dialogs.Views;

public sealed partial class IdentifyDialog : IViewFor<IdentifyViewModel>
{
	public IdentifyViewModel? ViewModel { get; set; } = App.GetService<IdentifyViewModel>();

	object? IViewFor.ViewModel { get => ViewModel; set => ViewModel = (IdentifyViewModel?)value; }

	public IdentifyDialog()
	{
		InitializeComponent();
	}

	private void ItemsView_SelectionChanged(Microsoft.UI.Xaml.Controls.ItemsView sender, Microsoft.UI.Xaml.Controls.ItemsViewSelectionChangedEventArgs args)
	{
		if (ViewModel is null)
		{
			return;
		}

		ViewModel.SelectedResult = (RemoteSearchResult)sender.SelectedItem;
	}

}
