using System.Collections.Generic;

namespace UsagiMQ.Configuration
{
    /// <summary>
    /// Represents the configuration settings for UsagiMQ.
    /// </summary>
    public class UsagiMQSettings
    {
        /// <summary>
        /// Gets or sets the RabbitMQ connection settings.
        /// </summary>
        public ConnectionSettings Connection { get; set; } = new() { HostName = "localhost", Password = "", UserName = "user", VirtualHost = "/" };

        /// <summary>
        /// Gets or sets the configuration for Dead Letter Exchange (DLX).
        /// </summary>
        public DLXSettings DLX { get; set; } = new();

        /// <summary>
        /// Gets or sets a flag indicating whether automatic queue creation is enabled.
        /// </summary>
        public bool AutoCreateQueues { get; set; } = true;
    }

    /// <summary>
    /// Represents the connection settings for RabbitMQ.
    /// </summary>
    public class ConnectionSettings
    {
        /// <summary>
        /// The hostname or IP address of the RabbitMQ server.
        /// </summary>
        public required string HostName { get; set; }

        /// <summary>
        /// The port number used to connect to RabbitMQ. Default is 5672.
        /// </summary>
        public int Port { get; set; } = 5672;

        /// <summary>
        /// The username for authentication with RabbitMQ.
        /// </summary>
        public required string UserName { get; set; }

        /// <summary>
        /// The password for authentication with RabbitMQ.
        /// </summary>
        public required string Password { get; set; }

        /// <summary>
        /// The requested heartbeat interval (in seconds) for the RabbitMQ connection.
        /// </summary>
        public int RequestedHeartbeat { get; set; } = 60;

        /// <summary>
        /// Indicates whether automatic recovery is enabled in case of connection failure.
        /// </summary>
        public bool AutomaticRecoveryEnabled { get; set; } = true;

        /// <summary>
        /// The interval (in milliseconds) for network recovery attempts.
        /// </summary>
        public int NetworkRecoveryInterval { get; set; } = 5000;

        /// <summary>
        /// The name of the virtual host.
        /// </summary>
        public required string VirtualHost { get; set; }

        /// <summary>
        /// The exchange used for commands.
        /// </summary>
        public string CommandsExchange { get; set; } = "exchange.commands";

        /// <summary>
        /// The queue used for handling commands.
        /// </summary>
        public string CommandsQueue { get; set; } = "queue.commands";

        /// <summary>
        /// The exchange used for events.
        /// </summary>
        public string EventsExchange { get; set; } = "exchange.events";
    }

    /// <summary>
    /// Represents the settings for Dead Letter Exchange (DLX).
    /// </summary>
    public class DLXSettings
    {
        /// <summary>
        /// Indicates whether DLX is enabled.
        /// </summary>
        public bool Enabled { get; set; } = false;

        /// <summary>
        /// The DLX exchange name.
        /// </summary>
        public string Exchange { get; set; } = "usagi.dlx";

        /// <summary>
        /// The DLX queue name.
        /// </summary>
        public string Queue { get; set; } = "usagi.dlx.queue";

        /// <summary>
        /// The routing key used for DLX.
        /// </summary>
        public string RoutingKey { get; set; } = "failed";
    }
}
