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

namespace FluentFin.Plugins.Playback_Reporting.ViewModels
{
	public partial class UsageReportViewModel : ObservableObject, INavigationAware
	{
		public WinUIPlot ByHour { get; } = new();
		public WinUIPlot ByDay { get; } = new();

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
				.SelectMany(x => PlaybackReportingHelper.GetHourlyReport(x.Days, x.EndDate))
				.Where(x => x.Count > 0)
				.ObserveOn(RxApp.MainThreadScheduler)
				.Subscribe(UpdatePlot);

			return Task.CompletedTask;
		}

		private void UpdatePlot(Dictionary<string, int> data)
		{
			CreateUsageByDay(data, ByDay);
			CreateUsageByHour(data, ByHour);
		}

		private static void CreateUsageByHour(Dictionary<string, int> data, WinUIPlot control)
		{
			control.UserInputProcessor.Disable();

			var plot = control.Plot;
			plot.Clear();
			plot.Title("Usage By Hour");
			plot.YLabel("Usage");
			plot.XLabel("Time");
			plot.ShowLegend();
			plot.Axes.Margins(bottom: 0);
			plot.Axes.Bottom.MajorTickStyle.Length = 0;

			List<Bar> bars = [];
			foreach (var kv in data)
			{
				int hour = int.Parse(kv.Key.Split("-")[1]);

				if (bars.FirstOrDefault(x => x.Position == hour) is { } barToUpdate)
				{
					barToUpdate.Value += kv.Value;
					barToUpdate.Label = TimeSpan.FromSeconds(barToUpdate.Value).ToString();
				}
				else
				{
					bars.Add(new Bar
					{
						Value = kv.Value,
						Position = hour,
						Label = TimeSpan.FromSeconds(kv.Value).ToString()
					});
				}
			}

			plot.Add.Bars(bars);

			plot.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericAutomatic()
			{
				LabelFormatter = (value) => DateTime.Today.Add(TimeSpan.FromHours(value)).ToString("h tt")
			};
			plot.Axes.Left.TickGenerator = new ScottPlot.TickGenerators.NumericAutomatic()
			{
				LabelFormatter = (value) => TimeSpan.FromSeconds(value).ToString()
			};
			plot.Axes.AutoScale();

			control.Refresh();
		}

		private static void CreateUsageByDay(Dictionary<string, int> data, WinUIPlot control)
		{
			control.UserInputProcessor.Disable();

			var plot = control.Plot;
			plot.Clear();
			plot.Title("Usage By Day");
			plot.YLabel("Usage");
			plot.XLabel("Day");
			plot.ShowLegend();
			plot.Axes.Margins(bottom: 0);
			plot.Axes.Bottom.MajorTickStyle.Length = 0;

			List<Bar> bars = [];
			foreach (var kv in data)
			{
				int day = int.Parse(kv.Key.Split("-")[0]);

				if (bars.FirstOrDefault(x => x.Position == day) is { } barToUpdate)
				{
					barToUpdate.Value += kv.Value;
					barToUpdate.Label = barToUpdate.Value > 0 ? TimeSpan.FromSeconds(barToUpdate.Value).ToString() : "";
				}
				else
				{
					bars.Add(new Bar
					{
						Value = kv.Value,
						Position = day,
						Label = kv.Value > 0 ? TimeSpan.FromSeconds(kv.Value).ToString() : ""
					});
				}
			}

			plot.Add.Bars(bars);

			Tick[] ticks =
			[
				new(0, "Sun"),
				new(1, "Mon"),
				new(2, "Tue"),
				new(3, "Wed"),
				new(4, "Thu"),
				new(5, "Fri"),
				new(6, "Sat"),
			];

			plot.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual(ticks);
			plot.Axes.Left.TickGenerator = new ScottPlot.TickGenerators.NumericAutomatic()
			{
				LabelFormatter = (value) => TimeSpan.FromSeconds(value).ToString()
			};
			plot.Axes.AutoScale();

			control.Refresh();
		}
	}
}
