namespace UsagiMQ.Core.Producers
{
    /// <summary>
    /// Factory for creating instances of <see cref="IUsagiMQProducer"/> dynamically.
    /// </summary>
    public interface IUsagiMQProducerFactory : IDisposable
    {
        /// <summary>
        /// Gets a new instance of <see cref="IUsagiMQProducer"/> for a given VirtualHost.
        /// </summary>
        /// <param name="virtualHost">The VirtualHost associated with the RabbitMQ connection.</param>
        /// <returns>An instance of <see cref="IUsagiMQProducer"/>.</returns>
        IUsagiMQProducer GetProducer(string virtualHost);
    }
}
