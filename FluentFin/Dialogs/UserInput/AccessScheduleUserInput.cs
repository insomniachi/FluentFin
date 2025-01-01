using FluentFin.Core.Contracts.Services;
using FluentFin.Dialogs.ViewModels;
using FluentFin.Services;
using Jellyfin.Sdk.Generated.Models;
using Microsoft.Extensions.DependencyInjection;

namespace FluentFin.Dialogs.UserInput;

public partial class AccessScheduleUserInput(IContentDialogService dialogService,
										     IServiceProvider serviceProvider) : IUserInput<AccessSchedule>
{
	public async Task<AccessSchedule?> GetValue()
	{
		var vm = serviceProvider.GetRequiredService<AccessSchedulePickerViewModel>();
		var result = await dialogService.ShowDialog(vm, null!);

		if(result == Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary)
		{
			return new AccessSchedule
			{
				DayOfWeek = vm.DayOfWeek,
				StartHour = vm.StartTime?.TotalHours,
				EndHour = vm.EndTime?.TotalHours
			};
		}

		return null;
	}
}
