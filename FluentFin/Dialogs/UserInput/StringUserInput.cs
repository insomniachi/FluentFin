using FluentFin.Core.Contracts.Services;
using FluentFin.Dialogs.ViewModels;
using FluentFin.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FluentFin.Dialogs.UserInput;

public abstract partial class StringUserInput(IContentDialogService dialogService,
									          IServiceProvider serviceProvider) : IUserInput<string>
{
	public virtual string? Message { get; }
	public virtual string? Title { get; }
	public virtual string? PlaceholderText { get; }

	public async Task<string?> GetValue()
	{
		var vm = serviceProvider.GetRequiredService<StringPickerViewModel>();
		vm.Message = Message;
		vm.PlaceholderText = PlaceholderText;
		vm.Title = Title;

		var result = await dialogService.ShowDialog(vm, null!);

		if (result == Microsoft.UI.Xaml.Controls.ContentDialogResult.Primary)
		{
			return vm.Name;
		}

		return null;
	}
}

public sealed class LibraryNewNameInput(IContentDialogService dialogService, IServiceProvider serviceProvider) : StringUserInput(dialogService, serviceProvider)
{
	public override string? Message => "Renaming a media library will cause all metadata to be lost, proceed with caution.";
	public override string? Title => "Rename Library";
	public override string? PlaceholderText => "Enter new name";
}

public sealed class SendMessageToSessionInput(IContentDialogService dialogService, IServiceProvider serviceProvider) : StringUserInput(dialogService, serviceProvider)
{
	public override string? Title => "Send Message";
	public override string? PlaceholderText => "Enter message";
}
