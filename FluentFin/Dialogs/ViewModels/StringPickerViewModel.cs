using System.Reactive.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using ReactiveUI;

namespace FluentFin.Dialogs.ViewModels;

public partial class StringPickerViewModel : ObservableObject
{
	public StringPickerViewModel()
	{
		this.WhenAnyValue(x => x.Message)
			.Select(string.IsNullOrEmpty)
			.Subscribe(isEmpty => HasInfo = !isEmpty);
	}

	[ObservableProperty]
	public partial string? Name { get; set; }

	[ObservableProperty]
	public partial string? PlaceholderText { get; set; }

	[ObservableProperty]
	public partial string? Message { get; set; }

	[ObservableProperty]
	public partial string? Title { get; set; }

	[ObservableProperty]
	public partial bool HasInfo { get; set; }
}
