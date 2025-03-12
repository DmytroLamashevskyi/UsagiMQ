using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace UsagiMQ.Core.Consumers
{
    internal class UsagiMQBackgroundService : BackgroundService
    {
        /// <summary>
        /// Background service responsible for managing UsagiMQ consumers.
        /// </summary>
        private readonly IUsagiConsumerManager _consumerManager;
        private readonly ILogger<UsagiMQBackgroundService> _logger;

        /// <summary>
        /// Initializes the UsagiMQ background service.
        /// </summary>
        /// <param name="consumerManager">The consumer manager that controls message consumers.</param>
        /// <param name="logger">Logger instance for monitoring background service events.</param>
        public UsagiMQBackgroundService(IUsagiConsumerManager consumerManager, ILogger<UsagiMQBackgroundService> logger)
        {
            _consumerManager = consumerManager;
            _logger = logger;
        }

        /// <summary>
        /// Starts the background service and initializes consumer processing.
        /// </summary>
        /// <param name="cancellationToken">Token for stopping the service.</param>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("[UsagiMQBackgroundService] Starting background processing...");

            try
            {
                // Запускаем обработку сообщений в консумерах
                _logger.LogInformation("[UsagiMQBackgroundService] Initializing consumers...");
                await _consumerManager.RegisterConsumersAsync();
                _logger.LogInformation("[UsagiMQBackgroundService] Starting consumers...");
                await _consumerManager.StartListeningAsync(stoppingToken);

                while(!stoppingToken.IsCancellationRequested)
                {
                    // Проверяем работоспособность консумеров
                    bool areRunning = await _consumerManager.IsRunningAsync();
                    if(!areRunning)
                    {
                        _logger.LogWarning("[UsagiMQBackgroundService] Consumers stopped unexpectedly. Restarting...");
                        await _consumerManager.RestartListeningAsync(stoppingToken);
                    }

                    // Ждём перед следующей проверкой (например, каждые 30 секунд)
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "[UsagiMQBackgroundService] An error occurred in the background processing.");
            }
        }

        /// <summary>
        /// Stops the background service and gracefully shuts down consumers.
        /// </summary>
        /// <param name="cancellationToken">Token for stopping the service.</param>
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("[UsagiMQBackgroundService] Stopping background service...");
            await _consumerManager.StopListeningAsync();
            await base.StopAsync(cancellationToken);
        }
    }
}
