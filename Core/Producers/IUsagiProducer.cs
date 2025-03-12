namespace UsagiMQ.Core.Producers
{
    /// <summary>
    /// Defines the contract for an UsagiMQ producer, responsible for sending messages to RabbitMQ.
    /// </summary>
    public interface IUsagiMQProducer
    {
        /// <summary>
        /// Sends a command message to a specified queue and waits for a response.
        /// A temporary ReplyTo queue is used automatically.
        /// </summary>
        /// <typeparam name="TResponse">The expected response type.</typeparam>
        /// <typeparam name="TCommand">The command message type.</typeparam>
        /// <param name="queue">The target queue name.</param>
        /// <param name="command">The command message object.</param>
        /// <param name="timeoutMs">The response timeout in milliseconds. Default is 5000ms.</param>
        /// <param name="headers">Optional headers to include in the message.</param>
        /// <returns>A task representing the asynchronous operation, returning the response.</returns>
        /// <exception cref="TimeoutException">Thrown when the response timeout expires.</exception>
        /// <exception cref="UsagiMQException">Thrown when there is a RabbitMQ connection issue.</exception>
        /// <exception cref="JsonException">Thrown when there is a serialization or deserialization failure.</exception>
        Task<TResponse> SendAsync<TResponse, TCommand>(
            string queue,
            TCommand command,
            int timeoutMs = 5000,
            Dictionary<string, object>? headers = null);

        /// <summary>
        /// Publishes an event message to a specified exchange.
        /// </summary>
        /// <typeparam name="TEvent">The event message type.</typeparam>
        /// <param name="exchange">The target exchange name.</param>
        /// <param name="routingKey">The routing key for the event.</param>
        /// <param name="eventMessage">The event message object.</param>
        /// <param name="headers">Optional headers to include in the message.</param>
        /// <param name="delayMs">Optional delay in milliseconds before the message is processed.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="UsagiMQException">Thrown when there is a RabbitMQ connection issue.</exception>
        /// <exception cref="JsonException">Thrown when there is a serialization failure.</exception>
        Task NotifyAsync<TEvent>(
            string exchange,
            string routingKey,
            TEvent eventMessage,
            Dictionary<string, object>? headers = null,
            int? delayMs = null);
    }
}
