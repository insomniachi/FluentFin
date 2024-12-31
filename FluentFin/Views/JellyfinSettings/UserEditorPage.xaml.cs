using FluentFin.Contracts.Services;
using FluentFin.Core.ViewModels;
using FluentFin.Dialogs.ViewModels;
using Microsoft.UI.Xaml.Controls;
using ReactiveUI;

namespace FluentFin.Views;

public sealed partial class UserEditorPage : Page
{

    public UserEditorViewModel ViewModel { get; } = App.GetService<UserEditorViewModel>();
    
    public UserEditorPage()
    {
        InitializeComponent();

        var navigationService = App.GetKeyedService<INavigationService>("UserEditor");
        navigationService.Frame = NavFrame;

        ViewModel.WhenAnyValue(x => x.Section)
            .Subscribe(section =>
            {
                var item = SelectorBar.Items.FirstOrDefault(x => x.Tag is UserEditorSection s && s == section);
                if(item is not null)
                {
                    SelectorBar.SelectedItem = item;
				}
            });
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
