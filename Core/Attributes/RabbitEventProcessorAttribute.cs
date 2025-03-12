namespace UsagiMQ.Core.Attributes
{
    /// <summary>
    /// Specifies that a class is an event processor for a RabbitMQ exchange.
    /// This attribute is used by <see cref="UsagiConsumerManager"/> to register event listeners dynamically.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class RabbitEventProcessorAttribute : Attribute
    {
        /// <summary>
        /// Gets the name of the virtual host where the exchange resides.
        /// </summary>
        public string VirtualHost { get; }

        /// <summary>
        /// Gets the name of the RabbitMQ exchange that this processor listens to.
        /// </summary>
        public string ExchangeName { get; }

        /// <summary>
        /// Gets the type of exchange (fanout, topic, direct).
        /// </summary>
        public string ExchangeType { get; }

        /// <summary>
        /// Gets the type of exchange (fanout, topic, direct).
        /// </summary>
        public string Target { get; }

        /// <summary>
        /// Gets the type of exchange (fanout, topic, direct).
        /// </summary>
        public string Action { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="RabbitEventProcessorAttribute"/>.
        /// </summary>
        /// <param name="virtualHost">The virtual host in RabbitMQ.</param>
        /// <param name="exchangeName">The name of the exchange.</param>
        /// <param name="exchangeType">The type of the exchange (e.g., "fanout", "topic", "direct").</param>
        public RabbitEventProcessorAttribute(string virtualHost, string exchangeName, string action, string target,  string exchangeType = "fanout")
        {
            VirtualHost = virtualHost;
            ExchangeName = exchangeName;
            ExchangeType = exchangeType;
            Target = target;
            Action = action;
        }
    }
}
