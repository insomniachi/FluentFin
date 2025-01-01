using FluentFin.Contracts.Services;
using FluentFin.Core;
using FluentFin.Core.ViewModels;
using FluentFin.Dialogs.ViewModels;
using FluentFin.Helpers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using ReactiveUI;

namespace FluentFin.Views;

public sealed partial class UserEditorPage : Page
{
    private readonly INavigationService _navigationService = App.GetKeyedService<INavigationService>(NavigationRegions.UserEditor);
	public UserEditorViewModel ViewModel { get; } = App.GetService<UserEditorViewModel>();
    
    public UserEditorPage()
    {
        InitializeComponent();

		_navigationService.Frame = NavFrame;
		_navigationService.Navigated += NavigationService_Navigated;
    }

	public bool IsSelected(UserSectionEditorViewModel vm, UserEditorSection section)
	{
		if(vm is null)
		{
			return false;
		}

		return section switch
		{
			UserEditorSection.Profile => vm is UserProfileEditorViewModel,
			//UserEditorSection.Access => vm is UserAccessEditorViewModel,
			//UserEditorSection.ParentalControl => vm is UserParentalControlEditorViewModel,
			//UserEditorSection.Password => vm is UserPasswordEditorViewModel,
			_ => false
		};
	}

	private void NavigationService_Navigated(object sender, NavigationEventArgs e)
	{
		if (NavFrame.GetPageViewModel() is UserSectionEditorViewModel vm)
		{
			ViewModel.SelectedSectionViewModel = vm;
		}
	}

	protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
	{
		_navigationService.Navigated -= NavigationService_Navigated;
	}

	private void SelectorBar_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
	{
        if(ViewModel is null)
        {
            return;
        }


        if(sender.SelectedItem.Tag is UserEditorSection section)
        {
            ViewModel.Section = section;
        }
    }
}
