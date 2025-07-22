using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;

namespace RepportingApp.CoreSystem.ApiSystem
{
    public class ShutdownChecker
    {
        public static readonly string ConnectionString = "Endpoint=https://repporting.azconfig.io;Id=IOts;Secret=D30BaBHSUU1YfgnawceIU1Bnj6BztMo9lWXnKFaWrb0nRmyejKLWJQQJ99AIAC5RqLJpihN2AAACAZACOGBX";
        
        private static readonly Lazy<ShutdownChecker> _instance = new(() => new ShutdownChecker());
        public static ShutdownChecker Instance => _instance.Value;

        public IConfiguration Configuration { get; private set; }
        private IConfigurationRefresher _configurationRefresher;
        private CancellationTokenSource _cancellationTokenSource;
        private Task _monitoringTask;

        private ShutdownChecker()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            InitializeConfiguration();
        }

        private void InitializeConfiguration()
        {
            try
            {
                var builder = new ConfigurationBuilder()
                    .AddAzureAppConfiguration(options =>
                    {
                        options.Connect(ConnectionString)
                            .Select(KeyFilter.Any, "new system")
                            .ConfigureRefresh(refresh =>
                            {
                                refresh.Register("NewSystemBeta", "new system")
                                    .SetCacheExpiration(TimeSpan.FromHours(8)); // Changed from 48 to 8 hours
                            });

                        _configurationRefresher = options.GetRefresher();
                    });

                Configuration = builder.Build();
                
                // Start monitoring immediately after configuration is built
                StartMonitoring();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing configuration: {ex.Message}");
                // Don't start monitoring if configuration failed to initialize
            }
        }

        public void StartMonitoring()
        {
            if (_monitoringTask == null || _monitoringTask.IsCompleted)
            {
                _monitoringTask = MonitorConfigurationAsync(_cancellationTokenSource.Token);
            }
        }

        public void StopMonitoring()
        {
            _cancellationTokenSource?.Cancel();
        }

        private async Task MonitorConfigurationAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("Starting configuration monitoring...");
            
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Check the date condition first
                    var specificDate = new DateTime(2025, 2, 1); // September 1, 2025
                    if (DateTime.Today < specificDate)
                    {
                        Console.WriteLine($"Monitoring disabled until {specificDate:yyyy-MM-dd}. Current date: {DateTime.Today:yyyy-MM-dd}");
                        await Task.Delay(TimeSpan.FromHours(24), cancellationToken); // Check daily
                        continue;
                    }

                    // Refresh the configuration
                    await _configurationRefresher.RefreshAsync(cancellationToken);
                    
                    // Get the current value
                    var mainValue = Configuration["NewSystemBeta"];
                    Console.WriteLine($"Current configuration value: '{mainValue}'");

                    // Check for shutdown condition (case-insensitive comparison)
                    if (string.Equals(mainValue, "Disable", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("Disable value detected. Initiating shutdown...");
                        await InitiateGracefulShutdown();
                        return; // Exit the monitoring loop
                    }
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("Configuration monitoring was cancelled.");
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error refreshing configuration: {ex.Message}");
                    // Continue monitoring even on error, but wait a bit longer
                    await Task.Delay(TimeSpan.FromMinutes(30), cancellationToken);
                    continue;
                }

                // Wait 8 hours before next check
                await Task.Delay(TimeSpan.FromHours(8), cancellationToken);
            }
        }

        private async Task InitiateGracefulShutdown()
        {
            try
            {
                Console.WriteLine("Attempting graceful application shutdown...");
                
                // Try to shutdown gracefully through Avalonia's application lifetime
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    try
                    {
                        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopApp)
                        {
                            Console.WriteLine("Shutting down desktop application...");
                            desktopApp.Shutdown(0);
                        }
                        else if (Application.Current?.ApplicationLifetime is IControlledApplicationLifetime controlledApp)
                        {
                            Console.WriteLine("Shutting down controlled application...");
                            controlledApp.Shutdown(0);
                        }
                        else
                        {
                            Console.WriteLine("No known application lifetime found, using Environment.Exit");
                            Environment.Exit(0);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error during UI thread shutdown: {ex.Message}");
                        Environment.Exit(0);
                    }
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during graceful shutdown: {ex.Message}");
                Console.WriteLine("Forcing application exit...");
                Environment.Exit(0);
            }
        }

        // Method to manually check configuration (useful for testing)
        public async Task<string> GetCurrentConfigValueAsync()
        {
            try
            {
                await _configurationRefresher.RefreshAsync();
                return Configuration["NewSystemBeta"];
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting current config value: {ex.Message}");
                return null;
            }
        }

        // Method to force a configuration check (useful for testing)
        public async Task ForceConfigurationCheckAsync()
        {
            try
            {
                await _configurationRefresher.RefreshAsync();
                var mainValue = Configuration["NewSystemBeta"];
                Console.WriteLine($"Forced check - Current value: '{mainValue}'");
                
                if (string.Equals(mainValue, "Disable", StringComparison.OrdinalIgnoreCase))
                {
                    await InitiateGracefulShutdown();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during forced configuration check: {ex.Message}");
            }
        }

        public void Dispose()
        {
            StopMonitoring();
            _cancellationTokenSource?.Dispose();
        }
    }
}