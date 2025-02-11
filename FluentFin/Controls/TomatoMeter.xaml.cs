using System.Reactive.Linq;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml.Controls;
using ReactiveUI;


namespace FluentFin.Controls;

public sealed partial class TomatoMeter : UserControl
{
	[GeneratedDependencyProperty]
	public partial float? Rating { get; set; }

	[GeneratedDependencyProperty]
	public partial TomatoMeterBadge Badge { get; set; }

	public TomatoMeter()
	{
		InitializeComponent();

		this.WhenAnyValue(x => x.Rating)
			.ObserveOn(RxApp.MainThreadScheduler)
			.Select(x => x > 60 ? TomatoMeterBadge.Fresh : TomatoMeterBadge.Rotten)
			.Subscribe(badge => Badge = badge);
	}
}

public enum TomatoMeterBadge
{
	Rotten,
	Fresh
}