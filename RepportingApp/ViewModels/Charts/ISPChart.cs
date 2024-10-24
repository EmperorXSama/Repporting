using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DataAccess.Models;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace RepportingApp.ViewModels.Charts;

public class ISPChart
{
    
    public ObservableCollection<ISeries> PieSeries;
    public void LoadEmailData()
    {
        var emails = DummyData.GetEmailsList();
        var stats = GetEmailStats(emails);
    
        
        PieSeries = new ObservableCollection<ISeries>
        {
            new PieSeries<double>
            {
                MaxRadialColumnWidth = 60,
                Values = new double[] { stats.FirstOrDefault(s => s.ISP == "gmail.com")?.Count ?? 0 },
                Name = "Gmail",
                DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
                DataLabelsPaint = new SolidColorPaint(SKColor.Parse("#E5E6E4")),
                DataLabelsSize = 24,
                Pushout = 1,
                Stroke = new SolidColorPaint(SKColor.Parse("#A6A2A2")),
                Fill = new SolidColorPaint(SKColor.Parse("#A6A2A2")),
            },
            new PieSeries<double>
            {
                MaxRadialColumnWidth = 60,
                Values = new double[] { stats.FirstOrDefault(s => s.ISP == "yahoo.com")?.Count ?? 0 },
                Name = "Yahoo",
                DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
                DataLabelsPaint = new SolidColorPaint(SKColor.Parse("#847577")),
                DataLabelsSize = 24,
                Pushout = 1,
                Stroke = new SolidColorPaint(SKColor.Parse("#A6A2A2")),
                Fill = new SolidColorPaint(SKColor.Parse("#CFD2CD")),
            },
            new PieSeries<double>
            {
                MaxRadialColumnWidth = 60,
                Values = new double[] { stats.FirstOrDefault(s => s.ISP == "att.net")?.Count ?? 0 },
                Name = "ATT",
                DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
                DataLabelsPaint = new SolidColorPaint(SKColor.Parse("#CFD2CD")),
                DataLabelsSize = 24,
                Pushout = 1,
                Stroke = new SolidColorPaint(SKColor.Parse("#A6A2A2")),
                Fill = new SolidColorPaint(SKColor.Parse("#847577")),
            }
        };
    }
    
    private List<EmailStats> GetEmailStats(List<EmailsCoreModel> emails)
    {
        var totalEmails = emails.Count;
        var ispGroups = emails.GroupBy(email => email.EmailAddress.Split('@')[1]);

        var stats = ispGroups.Select(group => new EmailStats
        {
            ISP = group.Key,
            Count = group.Count(),
            Percentage = (group.Count() / (double)totalEmails) * 100
        }).ToList();

        return stats;
    }
}