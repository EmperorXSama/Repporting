using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Reporting.lib.Models.Settings;

public class ReportingSettingValues
{
    private const string ConfigFilePath = "appsettings.json";
    public int Thread { get; set; }
    public int Repetition { get; set; }
    public int RepetitionDelay { get; set; }
    public int TimeUntilNextRun {get; set;}

    public ReportingSettingValues(IConfiguration configuration)
    {
        Thread = int.Parse(configuration["AppSettings:thread"] ?? "0");
        Repetition = int.Parse(configuration["AppSettings:repetition"] ?? "0");
        RepetitionDelay = int.Parse(configuration["AppSettings:repetition_delay"] ?? "0");
        TimeUntilNextRun = int.Parse(configuration["AppSettings:Interval"] ?? "0");
    }
    
    public async Task SaveConfigurationAsync()
    {
        // Load the current configuration from the file
        var json = await File.ReadAllTextAsync(ConfigFilePath);
        var jsonDocument = JsonDocument.Parse(json);

        // Convert the JSON to a dictionary so we can modify it
        var jsonObject = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, object>>>(json);

        if (jsonObject != null && jsonObject.ContainsKey("AppSettings"))
        {
            jsonObject["AppSettings"]["thread"] = Thread;
            jsonObject["AppSettings"]["repetition"] = Repetition;
            jsonObject["AppSettings"]["repetition_delay"] = RepetitionDelay;
        }

        // Write the updated JSON back to the file
        var updatedJson = JsonSerializer.Serialize(jsonObject, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(ConfigFilePath, updatedJson);
    }
}