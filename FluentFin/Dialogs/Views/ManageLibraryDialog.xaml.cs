using FluentFin.Core.Contracts.Services;
using FluentFin.Dialogs.ViewModels;
using ReactiveUI;

namespace FluentFin.Dialogs.Views;

public sealed partial class ManageLibraryDialog : IViewFor<ManageLibraryViewModel>
{
	public ManageLibraryViewModel? ViewModel { get; set; } = App.GetService<ManageLibraryViewModel>();

	object? IViewFor.ViewModel { get => ViewModel; set => ViewModel = (ManageLibraryViewModel?)value; }

	public ManageLibraryDialog()
	{
		InitializeComponent();
	}

	public static string RefreshIntervalToString(int value) => value switch
	{
		0 => "Never",
		int i => $"Every {i} Days"
	};

	private async void Button_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
	{
		var input = App.GetService<IServerFolderPicker>();
		var value = await input.PickFolder();
		if (string.IsNullOrEmpty(value))
		{
			return;
		}

		ViewModel?.Locations.Add(value);
	}
}
