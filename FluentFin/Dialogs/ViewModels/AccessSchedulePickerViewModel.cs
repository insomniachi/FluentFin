using CommunityToolkit.Mvvm.ComponentModel;
using Jellyfin.Sdk.Generated.Models;

namespace FluentFin.Dialogs.ViewModels;

public partial class AccessSchedulePickerViewModel : ObservableObject
{
	[ObservableProperty]
	public partial AccessSchedule_DayOfWeek DayOfWeek { get; set; }

	[ObservableProperty]
	public partial TimeSpan? StartTime { get; set; }

	[ObservableProperty]
	public partial TimeSpan? EndTime { get; set; }
}
