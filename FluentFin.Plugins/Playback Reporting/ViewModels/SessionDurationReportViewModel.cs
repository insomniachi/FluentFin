using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentFin.Contracts.ViewModels;
using ReactiveUI;
using ScottPlot;
using ScottPlot.WinUI;

namespace FluentFin.Plugins.Playback_Reporting.ViewModels;

public partial class SessionDurationReportViewModel : ObservableObject, INavigationAware
{
	public WinUIPlot PlotContainer { get; } = new();

	[ObservableProperty]
	public partial DateTimeOffset EndDate { get; set; } = TimeProvider.System.GetLocalNow();

	[ObservableProperty]
	public partial int NumberOfWeeks { get; set; } = 4;

	public IEnumerable<int> Weeks { get; } = Enumerable.Range(1, 11);

	public Task OnNavigatedFrom() => Task.CompletedTask;

	public Task OnNavigatedTo(object parameter)
	{
		this.WhenAnyValue(x => x.EndDate, x => x.NumberOfWeeks)
			.Select(x => new { EndDate = x.Item1, Days = x.Item2 * 7 })
			.SelectMany(x => PlaybackReportingHelper.GetDurationHistogram(x.Days, x.EndDate))
			.Where(x => x.Count > 0)
			.ObserveOn(RxApp.MainThreadScheduler)
			.Subscribe(UpdatePlot);

		return Task.CompletedTask;
	}

	private void UpdatePlot(Dictionary<string, int> data)
	{
		PlotContainer.UserInputProcessor.Disable();

		if (data.Count < 1)
		{
			return;
		}

		var plot = PlotContainer.Plot;
		plot.Clear();

		Customize(plot);

		var palette = new ScottPlot.Palettes.Category10();
		var bars = new List<Bar>();

		foreach (var kv in data)
		{
			var bin = int.Parse(kv.Key);

			if (bin < 0)
			{
				continue;
			}

			bars.Add(new Bar
			{
				Value = kv.Value,
				Label = kv.Value.ToString(),
				Position = bin,
			});
		}

		var ticks = bars.Index().Select(x => new Tick(x.Index, $"{x.Index * 5}-{x.Index * 5 + 4}")).ToArray();
		plot.Add.Bars(bars);
		plot.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual(ticks);
		plot.Axes.AutoScale();

		PlotContainer.Refresh();
	}

	private static void Customize(Plot plot)
	{
		plot.Title("Counts per 5 minute intervals");
		plot.YLabel("Count");
		plot.XLabel("Bins");
		plot.Axes.Bottom.MajorTickStyle.Length = 0;
		plot.Axes.Margins(bottom: 0);
	}
}
