using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FluentFin.Core.Contracts.Services;
using Jellyfin.Sdk.Generated.Models;
using ReactiveUI;
using System.Collections.ObjectModel;
using System.Reactive.Linq;

namespace FluentFin.Dialogs.ViewModels;

public partial class IdentifyViewModel : ObservableObject
{
	private readonly IJellyfinClient _jellyfinClient;

	public IdentifyViewModel(IJellyfinClient jellyfinClient)
	{
		_jellyfinClient = jellyfinClient;

		this.WhenAnyValue(x => x.ViewState)
			.Select(value => value switch
			{
				State.Result => "Submit",
				_ => "Search"
			})
			.Subscribe(text => PrimaryButtonText = text);

		this.WhenAnyValue(x => x.ViewState)
			.Select(value => value switch
			{
				State.Result => "Back",
				_ => ""
			})
			.Subscribe(text => SecondaryButtonText = text);
	}

	public BaseItemDto Item { get; set; } = new();

	[ObservableProperty]
	public partial string Name { get; set; } = "";

	[ObservableProperty]
	public partial int? Year { get; set; }

	[ObservableProperty]
	public partial List<RemoteSearchResult> Results { get; set; } = [];

	[ObservableProperty]
	public partial State ViewState { get; set; } = State.Input;

	[ObservableProperty]
	public partial string PrimaryButtonText { get; set; } = "Search";

	[ObservableProperty]
	public partial RemoteSearchResult? SelectedResult { get; set; }

	[ObservableProperty]
	public partial string SecondaryButtonText { get; set; } = "";

	public ObservableCollection<KeyValueViewModel> ExternalIds { get; } = [];

	public bool CanClose { get; set; }


	public async Task Initialize(BaseItemDto dto)
	{
		Item = dto;

		var externalIds = await _jellyfinClient.GetExternalIdInfo(dto);

		foreach (var item in externalIds)
		{
			if(string.IsNullOrEmpty(item.Key) || string.IsNullOrEmpty(item.Name))
			{
				continue;
			}

			ExternalIds.Add(new KeyValueViewModel(item.Name, item.Key, item.Type));
		}
	}

	[RelayCommand]
	public async Task PrimaryButtonExecute()
	{
		if(ViewState == State.Input)
		{
			await Search();
		}
		else if(ViewState == State.Result)
		{
			await Submit();
		}
	}

	[RelayCommand]
	public void Back() => ViewState = State.Input;

	private async Task Submit()
	{
		if(SelectedResult is null)
		{
			return;
		}

		await _jellyfinClient.ApplyRemoteResult(Item, SelectedResult);
	}

	private async Task Search()
	{
		ViewState = State.Loading;
		if (Item.Type == BaseItemDto_Type.Movie)
		{
			Results = await _jellyfinClient.IdentifyMovie(Item, new MovieInfo
			{
				Name = Name,
				Year = Year,
				ProviderIds = new MovieInfo_ProviderIds
				{
					AdditionalData = ExternalIds.ToDictionary(x => x.Key, x => (object?)x.Value)
				}
			});
		}
		else if (Item.Type == BaseItemDto_Type.Series)
		{
			Results = await _jellyfinClient.IdentifySeries(Item, new SeriesInfo
			{
				Name = Name,
				Year = Year,
				ProviderIds = new SeriesInfo_ProviderIds
				{
					AdditionalData = ExternalIds.ToDictionary(x => x.Key, x => (object?)x.Value)
				}
			});
		}
		ViewState = State.Result;
	}
}

public partial class KeyValueViewModel(string name, string key, ExternalIdInfo_Type? type) : ObservableObject
{
	public string DisplayName { get; } = name;
	public string Key { get; } = key;
	public ExternalIdInfo_Type? Type { get; } = type;

	[ObservableProperty]
	public partial string? Value { get; set; }
}

public enum State
{
	Input,
	Loading,
	Result
}
