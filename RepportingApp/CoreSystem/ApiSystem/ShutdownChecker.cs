using System.Text.Json;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;

namespace RepportingApp.CoreSystem.ApiSystem;

public class ShutdownChecker
{
    public  static readonly string ConnectionString = "Endpoint=https://repporting.azconfig.io;Id=IOts;Secret=D30BaBHSUU1YfgnawceIU1Bnj6BztMo9lWXnKFaWrb0nRmyejKLWJQQJ99AIAC5RqLJpihN2AAACAZACOGBX";
   private static readonly Lazy<ShutdownChecker> _instance = new(() => new ShutdownChecker());
    public static ShutdownChecker Instance => _instance.Value;

    public IConfiguration Configuration { get; private set; }
    private IConfigurationRefresher _configurationRefresher;

    private ShutdownChecker()
    {
        var builder = new ConfigurationBuilder()
            .AddAzureAppConfiguration(options =>
            {
                options.Connect(ConnectionString)
                    .Select(KeyFilter.Any, "new system")
                    .ConfigureRefresh(refresh =>
                    {
                        refresh.Register("NewSystemBeta", "new system")
                            .SetCacheExpiration(TimeSpan.FromHours(48));
                    });

                _configurationRefresher = options.GetRefresher();
            });
        var specificDate = new DateTime(2025, 12, 1);
        if (DateTime.Today > specificDate)
        {
            Configuration = builder.Build();
        }
        
        // Start monitoring configuration changes
        _ = MonitorConfigurationAsync(CancellationToken.None); // You can pass a cancellation token if needed
    }

    private async Task MonitorConfigurationAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Refresh the configuration periodically
                await _configurationRefresher.RefreshAsync();
                var mainValue = Configuration["NewSystemBeta"];

                if (mainValue == "Disable")
                {
                    // Graceful shutdown if "Disable" is detected
                    await Task.Delay(4000);
                    Console.WriteLine("Shutting down application...");
                    Environment.Exit(0); // Or use a more graceful method
                }
            }
            catch (Exception ex)
            {
                // Log or handle the error appropriately
                Console.WriteLine($"Error refreshing configuration: {ex.Message}");
            }

            // Wait before the next refresh attempt
            await Task.Delay(TimeSpan.FromHours(48), cancellationToken); // Adjust interval as needed
        }
    }
}