using CommunityToolkit.Mvvm.ComponentModel;
using FluentFin.Core.Contracts.Services;
using Jellyfin.Sdk.Generated.Models;
using ReactiveUI;
using System.Reactive.Linq;

namespace FluentFin.Core.ViewModels;

public partial class UserAccessEditorViewModel(IJellyfinClient jellyfinClient) : UserSectionEditorViewModel
{
	[ObservableProperty]
	public partial bool EnableAllFolders { get; set; }

	[ObservableProperty]
	public partial List<MediaFolderViewModel> MediaFolders { get; set; } = [];

	protected override async Task Initialize(UserDto user)
	{
		if(User?.Policy is not { } policy)
		{
			return;
		}

		EnableAllFolders = policy.EnableAllFolders ?? false;

		var response = await jellyfinClient.GetMediaFolders();

		if (response is { Items.Count: > 0 })
		{
			MediaFolders = response.Items.Select(x =>
			{
				var model = new MediaFolderViewModel(user)
				{
					Name = x.Name,
					Id = x.Id,
					IsSelected = policy.EnabledFolders?.Contains(x.Id) == true
				};

				return model;
			}).ToList();
		}
	}
}

public partial class MediaFolderViewModel : ObservableObject
{
	public MediaFolderViewModel(UserDto user)
	{
		this.WhenAnyValue(x => x.IsSelected)
			.Subscribe(selected =>
			{
				if (selected && user.Policy?.EnabledFolders?.Contains(Id) == false)
				{
					user.Policy?.EnabledFolders?.Add(Id);
				}
				else if (!selected && user.Policy?.EnabledFolders?.Contains(Id) == true)
				{
					user.Policy?.EnabledFolders?.Remove(Id);
				}
			});
	}


	[ObservableProperty]
	public partial string? Name { get; set; }

	[ObservableProperty]
	public partial Guid? Id { get; set; }

	[ObservableProperty]
	public partial bool IsSelected { get; set; }
}
