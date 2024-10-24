using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using DataAccess.Models;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace RepportingApp.ViewModels.Charts;

public class ErrorProgressChart
{
    public ObservableCollection<ISeries> Series { get; private set; }
    public ObservableCollection<Axis> XAxes { get; private set; }
    public ObservableCollection<Axis> YAxes { get; private set; }

    public ErrorProgressChart()
    {
        DummyData.LoadDummyData();
    }
    public void GenerateChartData()
    {
        
        var processes = DummyData.Processes;

        XAxes = new ObservableCollection<Axis>
        {
            new Axis
            {
                Labels = processes.Select(p => p.ProcessDate.ToString("MMM dd")).ToArray()
            }
        };

        // Y Axis for counting
        YAxes = new ObservableCollection<Axis>
        {
            new Axis
            {
                Labeler = value => value.ToString("N0") // Formatting the values
            }
        };

        // Line series for SuccessCount, ProxyErrorCount, HttpErrorCount
        Series = new ObservableCollection<ISeries>
        {
            new LineSeries<int>
            {
                Values = processes.Select(p => p.SuccessCount).ToArray(),
                Name = "Successes",
                Stroke = new SolidColorPaint(SKColors.Green),
                Fill = null // No fill for line chart
            },
            new LineSeries<int>
            {
                Values = processes.Select(p => p.ProxyErrorCount).ToArray(),
                Name = "Proxy Errors",
                Stroke = new SolidColorPaint(SKColors.Red),
                Fill = null
            },
            new LineSeries<int>
            {
                Values = processes.Select(p => p.HttpErrorCount).ToArray(),
                Name = "HTTP Errors",
                Stroke = new SolidColorPaint(SKColors.Orange),
                Fill = null
            }
        };
    }

    public void FilterData(string timePeriod)
    {
        var filteredProcesses = DummyData.Processes;

        if (timePeriod == "Today")
        {
            filteredProcesses = filteredProcesses
                .Where(p => p.ProcessDate.Date == DateTime.Today).ToList();
        }
        else if (timePeriod == "Yesterday")
        {
            filteredProcesses = filteredProcesses
                .Where(p => p.ProcessDate.Date == DateTime.Today.AddDays(-1)).ToList();
        }
        else if (timePeriod == "Last 7 days")
        {
            filteredProcesses = filteredProcesses
                .Where(p => p.ProcessDate >= DateTime.Today.AddDays(-7)).ToList();
        }

        UpdateChartData(filteredProcesses);
    }

    public void UpdateChartData(List<ProcessModel> processes)
    {
        // Update chart data similar to GenerateChartData()
        GenerateChartData();
    }
}