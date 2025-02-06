using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentFin.Contracts.ViewModels;
using FluentFin.Core.Contracts.Services;
using Jellyfin.Sdk.Generated.Models;

namespace FluentFin.Core.ViewModels;

public partial class ScheduledTasksViewModel(IJellyfinClient jellyfinClient) : ObservableObject, INavigationAware
{
	[ObservableProperty]
	public partial List<CategoryTaskPair> Tasks { get; set; } = new();

	public Task OnNavigatedFrom() => Task.CompletedTask;

	public async Task OnNavigatedTo(object parameter)
	{
		var tasks = await jellyfinClient.GetScheduledTasks();

		if (tasks is null)
		{
			return;
		}

		Tasks = tasks
			.Where(x => x.Category != null)
			.GroupBy(x => x.Category!)
			.OrderBy(x => x.Key)
			.Select(x => new CategoryTaskPair(x.Key, [.. x]))
			.ToList();
	}

	[RelayCommand]
	private async Task Run(TaskInfo task)
	{
		if(string.IsNullOrEmpty(task.Id))
		{
			return;
		}

		await jellyfinClient.RunScheduledTask(task.Id);
	}
}

public record CategoryTaskPair(string Category, List<TaskInfo> Tasks);

