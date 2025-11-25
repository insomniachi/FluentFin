using System.Reactive.Concurrency;
using System.Reactive.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentFin.Core.Contracts.Services;
using Jellyfin.Sdk.Generated.Models;
using ReactiveUI;

namespace FluentFin.ViewModels;

public partial class TrickplayViewModel : ObservableObject
{
	private readonly IJellyfinClient _jellyfinClient;
	private TrickplayOptions? _trickplayOptions;

	[ObservableProperty]
	public partial Uri? TileImage { get; set; }

	[ObservableProperty]
	public partial TimeSpan Position { get; set; }

	[ObservableProperty]
	public partial RectModel? Clip { get; set; }


	[ObservableProperty]
	public partial double Height { get; set; }

	[ObservableProperty]
	public partial double Width { get; set; }

	public TranslateModel Translate { get; } = new();

	public int Index { get; set; }

	public BaseItemDto? Item { get; private set; }

	public TrickplayViewModel(IJellyfinClient jellyfinClient)
	{
		_jellyfinClient = jellyfinClient;

		this.WhenAnyValue(x => x.Position)
			.Throttle(TimeSpan.FromMilliseconds(200))
			.ObserveOn(RxApp.MainThreadScheduler)
			.Subscribe(x =>
			{
				if (Item is null)
				{
					return;
				}

				if (_trickplayOptions is null)
				{
					return;
				}

				if (Height == 0 ||
				   Width == 0)
				{
					return;
				}

				if (_trickplayOptions.Interval is null ||
				   _trickplayOptions.TileHeight is null ||
				   _trickplayOptions.TileWidth is null)
				{
					return;
				}

				var curTime = Position.TotalMilliseconds;


				var currentTile = Math.Floor(curTime / _trickplayOptions.Interval.Value);
				var tileSize = (_trickplayOptions.TileWidth * _trickplayOptions.TileHeight).Value;
				var tileOffset = currentTile % tileSize;
				var index = (int)Math.Floor(currentTile / tileSize);
				var tileOffsetX = tileOffset % (int)_trickplayOptions.TileWidth;
				var tileOffsetY = Math.Floor(tileOffset / (int)_trickplayOptions.TileHeight);

				if (TileImage is null || Index != index)
				{
					RxApp.MainThreadScheduler.Schedule(() =>
					{
						TileImage = jellyfinClient.GetTrickplayImage(Item, index, (int)Width);
					});
				}

				Index = index;

				RxApp.MainThreadScheduler.Schedule(() =>
				{
					Translate.X = -tileOffsetX * Width;
					Translate.Y = -tileOffsetY * Height;
					Clip = new(tileOffsetX * Width, tileOffsetY * Height, Width, Height);
				});
			});
	}


	public async Task Initialize()
	{
		var serverConfiguration = await _jellyfinClient.GetConfiguration();
		if (serverConfiguration?.TrickplayOptions is not { } trickplay)
		{
			return;
		}

		_trickplayOptions = trickplay;
		var width = _trickplayOptions.WidthResolutions?.FirstOrDefault();

		if (width is { } value)
		{
			Width = value;
		}
	}

	public void SetItem(BaseItemDto item)
	{
		Item = item;

		if (Width == 0)
		{
			return;
		}

		var aspect = Item.MediaStreams?.FirstOrDefault(x => x.Type == MediaStream_Type.Video)?.AspectRatio;

		if (string.IsNullOrEmpty(aspect))
		{
			return;
		}

		var split = aspect.Split(':', StringSplitOptions.RemoveEmptyEntries);
		if (split.Length != 2)
		{
			return;
		}

		if (!int.TryParse(split[0], out var aspectWidth))
		{
			return;
		}

		if (!int.TryParse(split[1], out var aspectHeight))
		{
			return;
		}

		Height = aspectHeight * Width / aspectWidth;
	}
}

public record RectModel(double X, double Y, double Width, double Height);
public partial class TranslateModel : ObservableObject
{
	[ObservableProperty]
	public partial double X { get; set; }

	[ObservableProperty]
	public partial double Y { get; set; }
}
