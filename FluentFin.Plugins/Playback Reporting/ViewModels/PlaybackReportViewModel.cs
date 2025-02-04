﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentFin.Contracts.ViewModels;
using ScottPlot;
using ScottPlot.WinUI;

namespace FluentFin.Plugins.Playback_Reporting.ViewModels;

public partial class PlaybackReportViewModel : ObservableObject, INavigationAware
{
	[ObservableProperty]
	public partial WinUIPlot? PlotContainer { get; set; }

	public Task OnNavigatedFrom() => Task.CompletedTask;

	public async Task OnNavigatedTo(object parameter)
	{
		PlotContainer = new WinUIPlot();
		var plot = PlotContainer.Plot;
		plot.Clear();

		PlotContainer.UserInputProcessor.Disable();
		var data = await PlaybackReportingHelper.GetPlayActivity(28, TimeProvider.System.GetUtcNow());

		if (data.Count < 1)
		{
			return;
		}

		var palette = new ScottPlot.Palettes.Category10();
		var bars = new List<Bar>();
		var legends = new List<LegendItem>();

		Customize(plot);

		foreach (var user in data.SkipLast(1))
		{
			legends.Add(new LegendItem
			{
				LabelText = user.UserName,
				FillColor = palette.GetColor(data.IndexOf(user)),
				LineWidth = 4
			});

			for (int i = 0; i < user.UserUsage.Count; i++)
			{
				var value = user.UserUsage.Values.ElementAt(i);

				if(value == 0)
				{
					continue;
				}

				var bar = new Bar
				{
					Value = value,
					FillColor = palette.GetColor(data.IndexOf(user)),
					Label = $"{value}",
					Position = i,
					CenterLabel = true,
					ValueBase = 0
				};

				if (bars.LastOrDefault(x => x.Position == i) is { } prevBar)
				{
					bar.ValueBase = prevBar.ValueBase + prevBar.Value;
					bar.Value += bar.ValueBase;
				}

				bars.Add(bar);
			}
		}


		plot.Add.Bars(bars);
		Tick[] ticks = data[0].UserUsage.Keys.Index().Select(x => new Tick(x.Index, x.Item)).ToArray();
		plot.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual(ticks);
		plot.ShowLegend(legends, Alignment.UpperRight);
		plot.Axes.AutoScale();
		
		PlotContainer.Refresh();
	}

	private static void Customize(Plot plot)
	{
		plot.Title("User Playback Report (Play Count)");
		plot.XLabel("Days");
		plot.YLabel("Count");
		plot.Axes.Bottom.MajorTickStyle.Length = 0;
		plot.Axes.Margins(bottom: 0);
	}
}
