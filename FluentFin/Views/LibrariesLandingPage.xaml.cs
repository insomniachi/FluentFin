using FluentFin.Core.ViewModels;
using Jellyfin.Sdk.Generated.Models;
using Microsoft.UI.Xaml.Controls;

namespace FluentFin.Views
{
	public sealed partial class LibrariesLandingPage : Page
	{
		public LibrariesLandingPageViewModel ViewModel { get; } = App.GetService<LibrariesLandingPageViewModel>();

		public LibrariesLandingPage()
		{
			InitializeComponent();
		}

		private void ItemsView_ItemInvoked(ItemsView sender, ItemsViewItemInvokedEventArgs args)
		{
			if (args.InvokedItem is not BaseItemDto library)
			{
				return;
			}

			ViewModel.NavigateToLibrary(library);
		}
	}
}
