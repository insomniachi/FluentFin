using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentFin.Core.Contracts.Services;

namespace FluentFin.Dialogs.ViewModels;

public partial class QuickConnectViewModel(IJellyfinClient jellyfinClient) : ObservableObject, IHandleClose
{
	[ObservableProperty]
	public partial string QuickConnectCode { get; set; }
	public bool CanClose { get; set; }

	[RelayCommand]
	private async Task VerifyQuickConnect()
	{
		if (string.IsNullOrEmpty(QuickConnectCode))
		{
			return;
		}

		await jellyfinClient.Authenticate(QuickConnectCode.Trim());
	}
}
