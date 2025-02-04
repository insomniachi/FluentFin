using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentFin.Contracts.ViewModels;
using ScottPlot;
using ScottPlot.WinUI;

namespace FluentFin.Plugins.Playback_Reporting.ViewModels;

public partial class SessionDurationReportViewModel : ObservableObject, INavigationAware
{
	public WinUIPlot PlotContainer { get; } = new();

	public Task OnNavigatedFrom() => Task.CompletedTask;

	public async Task OnNavigatedTo(object parameter)
	{
		PlotContainer.UserInputProcessor.Disable();

		var data = await PlaybackReportingHelper.GetDurationHistogram(28, TimeProvider.System.GetLocalNow());

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

			if(bin < 0)
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

		var ticks = bars.Index().Select(x => new Tick(x.Index, $"{x.Index}-{x.Index + 4}")).ToArray();
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
