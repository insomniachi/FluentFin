using FluentFin.Core.Contracts.Services;
using FluentFin.Dialogs.ViewModels;
using FluentFin.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FluentFin.Dialogs.UserInput;

public partial class StringUserInput(IContentDialogService dialogService,
									 IServiceProvider serviceProvider) : IUserInput<string>
{
	public async Task<string?> GetValue()
	{
		var vm = serviceProvider.GetRequiredService<StringPickerViewModel>();
		var result = await dialogService.ShowDialog(vm, null!);

		if (result == Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary)
		{
			return vm.Name;
		}

		return null;
	}
}
