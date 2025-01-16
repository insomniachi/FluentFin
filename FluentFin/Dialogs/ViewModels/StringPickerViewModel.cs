using CommunityToolkit.Mvvm.ComponentModel;

namespace FluentFin.Dialogs.ViewModels;

public partial class StringPickerViewModel : ObservableObject
{
	[ObservableProperty]
	public partial string Name { get; set; }
}
