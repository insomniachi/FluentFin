using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentFin.Contracts.ViewModels;
using ReactiveUI;
using ScottPlot;
using ScottPlot.WinUI;

namespace FluentFin.Plugins.Playback_Reporting.ViewModels;

public partial class BreakdownReportViewModel : ObservableObject, INavigationAware
{
	public ObservableCollection<WinUIPlot> Plots { get; } = [];

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
			.ObserveOn(RxApp.MainThreadScheduler)
			.Do(_ => Plots.Clear())
			.Subscribe( async x =>
			{
				await CreatePlot("GetTvShowsReport", x.Days, x.EndDate);
				await CreatePlot("MoviesReport", x.Days, x.EndDate);
				await CreatePlot("UserId", x.Days, x.EndDate);
				await CreatePlot("ItemType", x.Days, x.EndDate);
				await CreatePlot("PlaybackMethod", x.Days, x.EndDate);
				await CreatePlot("ClientName", x.Days, x.EndDate);
				await CreatePlot("DeviceName", x.Days, x.EndDate);
			});

		return Task.CompletedTask;
	}

	private async Task CreatePlot(string type, int days, DateTimeOffset endDate)
	{
		var countControl = new WinUIPlot();
		var timeControl = new WinUIPlot();
		Plots.Add(countControl);
		Plots.Add(timeControl);

		countControl.UserInputProcessor.Disable();
		timeControl.UserInputProcessor.Disable();

		var countPlot = countControl.Plot;
		var timePlot = timeControl.Plot;

		CustomizePlot(countPlot, "Count");
		CustomizePlot(timePlot, "Time");	

		var data = await PlaybackReportingHelper.GetBreakdown(type, days, endDate);

		var pallet = new ScottPlot.Palettes.Category10();
		var maxLength = data.Max(x => x.Label.Length);

		var countSlices = data.Index().Select(x => new PieSlice
		{
			Value = x.Item.Count,
			Label = x.Item.Count.ToString(),
			LegendText = $"{x.Item.Label} ({x.Item.Count})",
			FillColor = pallet.GetColor(x.Index)
		});

		var timeSlices = data.Index().Where(x => x.Item.Time > 0).Select(x => new PieSlice
		{
			Value = x.Item.Time,
			Label = TimeSpan.FromSeconds(x.Item.Time).ToString(),
			LegendText = $"{x.Item.Label} ({TimeSpan.FromSeconds(x.Item.Time)})",
			FillColor = pallet.GetColor(x.Index),
		});

		var countPie = countPlot.Add.Pie(countSlices.ToList());
		var timePie = timePlot.Add.Pie(timeSlices.ToList());

		countPlot.Axes.AutoScale();
		timePlot.Axes.AutoScale();
		
		countPie.SliceLabelDistance = 0.5;
		countPie.ExplodeFraction = .1;
		timePie.SliceLabelDistance = 0.5;
		timePie.ExplodeFraction = .1;

		countControl.Refresh();
		timeControl.Refresh();

		void CustomizePlot(Plot plot, string by)
		{
			plot.Title($"{type} ({by})");
			plot.ShowLegend();
			plot.Axes.Frameless(true);
			plot.HideGrid();
		}
	}
}


