using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace UsagiMQ.Core.Consumers
{
    /// <summary>
    /// Background service responsible for managing UsagiMQ consumers.
    /// </summary>
    internal class UsagiMQBackgroundService : BackgroundService
    {
        private readonly UsagiMQProcessor _processor;
        private readonly ILogger<UsagiMQBackgroundService> _logger;

        /// <summary>
        /// Initializes the UsagiMQ background service.
        /// </summary>
        /// <param name="processor">The processor responsible for handling message processing.</param>
        /// <param name="logger">Logger instance for monitoring background service events.</param>
        public UsagiMQBackgroundService(UsagiMQProcessor processor, ILogger<UsagiMQBackgroundService> logger)
        {
            _processor = processor;
            _logger = logger;
        }

        /// <summary>
        /// Starts the background service and initializes the RabbitMQ infrastructure.
        /// </summary>
        /// <param name="stoppingToken">Token for stopping the service.</param>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("[UsagiMQBackgroundService] Starting UsagiMQ background service...");
            try
            {
                // Инициализация процессора (сканирование сборок, регистрация консумеров и обработчиков)
                _processor.Initialize();

                // Настройка RabbitMQ (создание очередей, обменников, подписка)
                await _processor.SetupRabbitMQAsync();

                _logger.LogInformation("[UsagiMQBackgroundService] UsagiMQ is now listening for messages.");

                while(!stoppingToken.IsCancellationRequested)
                {
                    // Можно добавить проверку состояния процессора, если потребуется
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "[UsagiMQBackgroundService] An error occurred in the background processing.");
            }
        }

        /// <summary>
        /// Stops the background service gracefully.
        /// </summary>
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("[UsagiMQBackgroundService] Stopping UsagiMQ background service...");
            await base.StopAsync(cancellationToken);
        }
    }
}
