using RabbitMQ.Client.Events;

namespace UsagiMQ.Core.Consumers
{
    /// <summary>
    /// Manages the lifecycle of UsagiMQ consumers, including startup, shutdown, and monitoring.
    /// </summary>
    public interface IUsagiConsumerManager
    {
        /// <summary>
        /// Scans the assembly and registers all consumers and event processors.
        /// </summary>
        Task RegisterConsumersAsync();

        /// <summary>
        /// Starts listening for messages and processing incoming commands.
        /// </summary>
        Task StartListeningAsync(CancellationToken stoppingToken);

        /// <summary>
        /// Stops message processing and releases RabbitMQ resources.
        /// </summary>
        Task StopListeningAsync();

        /// <summary>
        /// Restarts message processing, reloading consumers if necessary.
        /// </summary>
        Task RestartListeningAsync(CancellationToken stoppingToken);

        /// <summary>
        /// Checks whether the consumers are currently running.
        /// </summary>
        Task<bool> IsRunningAsync();
    }

}
