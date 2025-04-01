﻿using System.Collections.ObjectModel;
using System.Reactive.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using FluentFin.Core.Contracts.Services;
using Jellyfin.Sdk.Generated.Models;
using ReactiveUI;

namespace FluentFin.Dialogs.ViewModels;

public partial class EditImagesViewModel(IJellyfinClient jellyfinClient) : ObservableObject, IHandleClose, IBaseItemDialogViewModel
{
	[ObservableProperty]
	public partial ObservableCollection<ImageInfoEx> Images { get; set; } = [];

	[ObservableProperty]
	public partial BaseItemDto? Item { get; set; }

	[ObservableProperty]
	public partial EditImagesViewModelState State { get; set; } = EditImagesViewModelState.Display;

	[ObservableProperty]
	public partial List<ImageProviderInfo> ImageProviders { get; set; } = [];

	[ObservableProperty]
	public partial ImageProviderInfo? SelectedProvider { get; set; }

	[ObservableProperty]
	public partial ImageType? SelectedImageType { get; set; }

	[ObservableProperty]
	public partial List<RemoteImageInfo> SearchResults { get; set; } = [];

	[ObservableProperty]
	public partial bool IncludeAllLanguages { get; set; }

	[ObservableProperty]
	public partial string BackButtonText { get; set; } = "";

	public ObservableCollection<ImageType?> ImageTypes { get; } = [];

	public bool CanClose { get; set; }

	public async Task Initialize(BaseItemDto item)
	{
		if (item is null)
		{
			return;
		}

		State = EditImagesViewModelState.Display;

		Item = item;

		var images = (await jellyfinClient.GetImages(item)).Select(info => new ImageInfoEx
		{
			ImageIndex = info.ImageIndex,
			ImageTag = info.ImageTag,
			Size = info.Size,
			ImageType = info.ImageType,
			Height = info.Height,
			Width = info.Width,
			Uri = jellyfinClient.GetImage(item, info)
		});

		Images = [.. images];


		var providers = await jellyfinClient.GetImageProviders(item);
		var supportedImages = GetAllSupportedImages(providers);
		ImageProviders = [new() { Name = "All", SupportedImages = supportedImages }, .. providers];
		SelectedProvider = ImageProviders.FirstOrDefault();
		SelectedImageType = supportedImages.FirstOrDefault();

		this.WhenAnyValue(x => x.SelectedProvider)
			.Where(x => x is { SupportedImages.Count: > 0 })
			.ObserveOn(RxApp.MainThreadScheduler)
			.Subscribe(provider =>
			{
				var newTypes = provider!.SupportedImages!.Except(ImageTypes).ToList();
				var removeTypes = ImageTypes.Except(provider.SupportedImages!).ToList();
				var selectedImageType = SelectedImageType;

				foreach (var type in removeTypes)
				{
					ImageTypes.Remove(type);
				}

				ImageTypes.AddRange(newTypes);

				if (ImageTypes.Contains(selectedImageType) == false)
				{
					SelectedImageType = ImageTypes.FirstOrDefault();
				}
			});

		this.WhenAnyValue(x => x.SelectedProvider, x => x.SelectedImageType, x => x.IncludeAllLanguages)
			.Where(x => x is { Item1: not null, Item2: not null })
			.Where(_ => State == EditImagesViewModelState.Search)
			.Subscribe(async _ => await SearchImages());

		this.WhenAnyValue(x => x.State)
			.Subscribe(state => BackButtonText = state == EditImagesViewModelState.Search ? "Back" : "");
	}

	private async Task SearchImages()
	{
		if (Item is null || SelectedImageType is null || SelectedProvider is null)
		{
			return;
		}

		State = EditImagesViewModelState.Loading;

		var providerName = SelectedProvider.Name == "All" ? null : SelectedProvider.Name;
		var result = await jellyfinClient.SearchImages(Item, SelectedImageType.Value, providerName, IncludeAllLanguages);

		if (result is null or { Images: null })
		{
			return;
		}

		SearchResults = result.Images;

		State = EditImagesViewModelState.Search;
	}

	[RelayCommand]
	private async Task SwitchView(EditImagesViewModelState state)
	{
		State = state;
		await SearchImages();
	}

	[RelayCommand]
	private async Task Search(ImageInfo info)
	{
		if (info.ImageType is { })
		{
			SelectedImageType = (ImageType?)(int)info.ImageType;
		}

		await SearchImages();
	}

	[RelayCommand]
	private async Task UpdateImage(RemoteImageInfo info)
	{
		if (Item is null)
		{
			return;
		}
		await jellyfinClient.UpdateImage(Item, info);
		await Initialize(Item);
	}

	[RelayCommand]
	private async Task DeleteImage(ImageInfoEx info)
	{
		if (Item is null || info is null)
		{
			return;
		}

		await jellyfinClient.DeleteImage(Item, info);
		Images.Remove(info);
	}

	private static List<ImageType?> GetAllSupportedImages(IEnumerable<ImageProviderInfo> providers)
	{
		HashSet<ImageType?> supportedImages = [];
		foreach (var p in providers)
		{
			if (p.SupportedImages is null)
			{
				continue;
			}

			foreach (var imageType in p.SupportedImages)
			{
				if (imageType is null)
				{
					continue;
				}

				supportedImages.Add(imageType);
			}
		}

		return [.. supportedImages];
	}
}

public enum EditImagesViewModelState
{
	Display,
	Search,
	Loading
}

public class ImageInfoEx : ImageInfo
{
	public Uri? Uri { get; set; }
}
