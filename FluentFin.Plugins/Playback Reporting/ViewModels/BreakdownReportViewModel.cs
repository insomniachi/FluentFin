using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using FluentFin.Contracts.ViewModels;
using ScottPlot;
using ScottPlot.WinUI;

namespace FluentFin.Plugins.Playback_Reporting.ViewModels;

public partial class BreakdownReportViewModel : ObservableObject, INavigationAware
{
	public ObservableCollection<WinUIPlot> Plots { get; } = [];
	public Task OnNavigatedFrom() => Task.CompletedTask;

	public async Task OnNavigatedTo(object parameter)
	{
		await CreatePlot("GetTvShowsReport");
		await CreatePlot("MoviesReport");
		await CreatePlot("UserId");
		await CreatePlot("ItemType");
		await CreatePlot("PlaybackMethod");
		await CreatePlot("ClientName");
		await CreatePlot("DeviceName");
	}


	private async Task CreatePlot(string type)
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

		var data = await PlaybackReportingHelper.GetBreakdown(type, 28, TimeProvider.System.GetUtcNow());

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


