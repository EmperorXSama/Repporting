using System.Collections.ObjectModel;
using System.Linq;
using DataAccess.Models;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace RepportingApp.ViewModels.Charts;

public class SpamCountChart
{
    public ObservableCollection<ISeries> SpamSeries { get; set; }
    public ObservableCollection<Axis> SpamXAxes { get; set; }
    public ObservableCollection<Axis> SpamYAxes { get; set; }
    
    public void GenerateSpamChartData()
    {
        var reportingProcesses = DummyData.Processes
            .Where(p => p.OperationName == "Reporting" && p.SpamCounts != null)
            .SelectMany(p => p.SpamCounts)
            .GroupBy(s => s.CountDate)
            .OrderBy(g => g.Key)
            .ToDictionary(g => g.Key.ToString("MMM dd"), g => g.Sum(s => s.SpamCount));

        SpamXAxes = new ObservableCollection<Axis>
        {
            new Axis
            {
                Labels = reportingProcesses.Keys.ToArray()
            }
        };

        SpamYAxes = new ObservableCollection<Axis>
        {
            new Axis
            {
                Labeler = value => value.ToString("N0")
            }
        };

        SpamSeries = new ObservableCollection<ISeries>
        {
            new LineSeries<int>
            {
                Values = reportingProcesses.Values.ToArray(),
                Name = "Spam Counts",
                Stroke = new SolidColorPaint(SKColors.Blue),
                Fill = null // No fill for line chart
            }
        };
    }

}