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

public partial class PlaybackReportViewModel : ObservableObject, INavigationAware
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
			.SelectMany(x => PlaybackReportingHelper.GetPlayActivity(x.Days, x.EndDate))
			.Where(x => x.Count > 0)
			.ObserveOn(RxApp.MainThreadScheduler)
			.Subscribe(UpdatePlot);

		return Task.CompletedTask;
	}

	private void UpdatePlot(List<PlayActivity> data)
	{
		var plot = PlotContainer.Plot;
		plot.Clear();

		PlotContainer.UserInputProcessor.Disable();

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

				if (value == 0)
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
