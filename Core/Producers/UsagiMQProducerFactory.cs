using RabbitMQ.Client;
using UsagiMQ.Configuration;

namespace UsagiMQ.Core.Producers
{
    /// <summary>
    /// Factory implementation for creating <see cref="IUsagiMQProducer"/> instances dynamically.
    /// Ensures that each producer instance is created per request and properly disposed.
    /// </summary>
    public class UsagiMQProducerFactory : IUsagiMQProducerFactory
    {
        private readonly ConnectionFactory _factory;
        private readonly IConnection _connection;
        private readonly object _lock = new();

        /// <summary>
        /// Initializes the factory with RabbitMQ connection settings.
        /// Uses a single connection for all VirtualHosts.
        /// </summary>
        /// <param name="settings">The UsagiMQ configuration settings.</param>
        public UsagiMQProducerFactory(UsagiMQSettings settings)
        {
            ArgumentNullException.ThrowIfNull(settings, nameof(settings));

            // Берем первую запись из настроек (все VirtualHost доступны через одного пользователя)
            var conn = settings.Connection;

            _factory = new ConnectionFactory
            {
                HostName = conn.HostName,
                Port = conn.Port,
                UserName = conn.UserName,
                Password = conn.Password,
                RequestedHeartbeat = TimeSpan.FromMilliseconds(conn.RequestedHeartbeat),
                AutomaticRecoveryEnabled = conn.AutomaticRecoveryEnabled,
                NetworkRecoveryInterval = TimeSpan.FromMilliseconds(conn.NetworkRecoveryInterval)
            };

            _connection = _factory.CreateConnectionAsync().GetAwaiter().GetResult();
        }

        /// <inheritdoc/>
        public IUsagiMQProducer GetProducer(string virtualHost)
        {
            lock(_lock)
            {
                if(!_connection.IsOpen)
                    throw new InvalidOperationException("RabbitMQ connection is closed.");

                return new UsagiMQProducer(_connection, virtualHost);
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _connection?.Dispose();
        }
    }
}
