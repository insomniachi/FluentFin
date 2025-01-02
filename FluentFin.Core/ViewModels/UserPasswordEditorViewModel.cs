using FluentFin.Core.Contracts.Services;

namespace FluentFin.Core.ViewModels;

public partial class UserPasswordEditorViewModel(IJellyfinClient jellyfinClient) : UserSectionEditorViewModel
{
	public async Task ChangePassword(string currentPassword, string newPassword)
	{
		await jellyfinClient.ChangePassword(User, currentPassword, newPassword);
	}

	public async Task ResetPassword() => await jellyfinClient.ResetPassword(User);
}
