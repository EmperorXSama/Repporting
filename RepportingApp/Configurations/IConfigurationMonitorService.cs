using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
 public interface IConfigurationMonitorService
    {
        Task StartMonitoringAsync();
        void StopMonitoring();
    }
namespace RepportingApp.Services
{
    public class AzureConfigurationMonitorService : BackgroundService, IConfigurationMonitorService
    {
        private readonly ILogger<AzureConfigurationMonitorService> _logger;
        private readonly string _connectionString;
        private readonly string _configKey;
        private readonly TimeSpan _checkInterval;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isMonitoring;

        public AzureConfigurationMonitorService(
            ILogger<AzureConfigurationMonitorService> logger,
            string connectionString,
            string configKey = "main",
            TimeSpan? checkInterval = null)
        {
            _logger = logger;
            _connectionString = connectionString;
            _configKey = configKey;
            _checkInterval = checkInterval ?? TimeSpan.FromHours(8);
            _cancellationTokenSource = new CancellationTokenSource();
        }

        public async Task StartMonitoringAsync()
        {
            if (_isMonitoring)
                return;

            _isMonitoring = true;
            _logger.LogInformation("Starting Azure Configuration monitoring for key: {ConfigKey}", _configKey);
            
            await StartAsync(_cancellationTokenSource.Token);
        }

        public void StopMonitoring()
        {
            _isMonitoring = false;
            _cancellationTokenSource?.Cancel();
            _logger.LogInformation("Stopped Azure Configuration monitoring");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Azure Configuration Monitor started. Checking every {Interval}", _checkInterval);

            while (!stoppingToken.IsCancellationRequested && _isMonitoring)
            {
                try
                {
                    await CheckConfigurationAsync(stoppingToken);
                    await Task.Delay(_checkInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Azure Configuration monitoring was cancelled");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while checking Azure Configuration");
                    // Continue monitoring even if there's an error
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); // Wait 5 minutes before retry
                }
            }
        }

        private async Task CheckConfigurationAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Starting configuration check...");
                
                // Create the configuration with timeout and optimizations
                var builder = new ConfigurationBuilder();
                builder.AddAzureAppConfiguration(options =>
                {
                    options.Connect(_connectionString)
                           .Select(_configKey, "new system") // Your label filter
                           .ConfigureClientOptions(clientOptions =>
                           {
                               // Set timeout to prevent hanging
                               clientOptions.Retry.NetworkTimeout = TimeSpan.FromSeconds(30);
                               clientOptions.Retry.MaxRetries = 2;
                               clientOptions.Retry.Delay = TimeSpan.FromSeconds(2);
                           });
                });

                // Use Task.Run to avoid blocking and add timeout
                var configurationTask = Task.Run(() => builder.Build(), cancellationToken);
                
                // Add a timeout to prevent hanging indefinitely
                var timeoutTask = Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
                var completedTask = await Task.WhenAny(configurationTask, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    _logger.LogWarning("Configuration build timed out after 1 minute");
                    throw new TimeoutException("Azure App Configuration build timed out");
                }

                var configuration = await configurationTask;
                _logger.LogInformation("Configuration built successfully");

                var configValue = configuration[_configKey];
                _logger.LogInformation("Current configuration value for '{ConfigKey}': '{Value}'", _configKey, configValue ?? "null");

                if (string.Equals(configValue, "disable", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Configuration key '{ConfigKey}' is set to 'disable'. Initiating application shutdown...", _configKey);
                    await InitiateShutdownAsync();
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Configuration check was cancelled");
                throw;
            }
            catch (TimeoutException ex)
            {
                _logger.LogError("Configuration check timed out: {Message}", ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check Azure App Configuration");
                throw;
            }
        }

        private async Task InitiateShutdownAsync()
        {
            try
            {
                _logger.LogInformation("Shutting down application due to Azure Configuration change");
                
                // Dispatch to UI thread for Avalonia
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopApp)
                    {
                        desktopApp.Shutdown(0);
                    }
                    else if (Application.Current?.ApplicationLifetime is IControlledApplicationLifetime controlledApp)
                    {
                        controlledApp.Shutdown(0);
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during application shutdown");
                // Force exit if graceful shutdown fails
                Environment.Exit(0);
            }
        }

        // Method to manually check configuration (useful for testing)
        public async Task<string> GetCurrentConfigValueAsync()
        {
            try
            {
                _logger.LogInformation("Getting current configuration value...");
                
                var builder = new ConfigurationBuilder();
                builder.AddAzureAppConfiguration(options =>
                {
                    options.Connect(_connectionString)
                           .Select(_configKey, "new system")
                           .ConfigureClientOptions(clientOptions =>
                           {
                               clientOptions.Retry.NetworkTimeout = TimeSpan.FromSeconds(30);
                               clientOptions.Retry.MaxRetries = 2;
                               clientOptions.Retry.Delay = TimeSpan.FromSeconds(2);
                           });
                });

                // Use Task.Run with timeout for this operation too
                var configurationTask = Task.Run(() => builder.Build());
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(45));
                var completedTask = await Task.WhenAny(configurationTask, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    _logger.LogWarning("Get current config value timed out");
                    return null;
                }

                var configuration = await configurationTask;
                var value = configuration[_configKey];
                _logger.LogInformation("Retrieved configuration value: '{Value}'", value ?? "null");
                
                return value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current config value");
                return null;
            }
        }

        // Method to force a configuration check (useful for testing)
        public async Task ForceConfigurationCheckAsync()
        {
            try
            {
                await CheckConfigurationAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during forced configuration check");
                throw;
            }
        }

        public override void Dispose()
        {
            StopMonitoring();
            _cancellationTokenSource?.Dispose();
            base.Dispose();
        }
    }
}