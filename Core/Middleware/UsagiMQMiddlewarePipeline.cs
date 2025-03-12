namespace UsagiMQ.Core.Middleware
{
    /// <summary>
    /// Represents the middleware pipeline for processing messages in UsagiMQ.
    /// Middleware components are executed in sequence before the message reaches its consumer.
    /// </summary>
    public class UsagiMQMiddlewarePipeline
    {
        private readonly List<Func<MessageContext, Func<Task>, Task>> _middlewareComponents = new();

        /// <summary>
        /// Adds a middleware component to the pipeline.
        /// Middleware should call the next delegate to continue processing.
        /// </summary>
        /// <typeparam name="T">The middleware type.</typeparam>
        /// <returns>The updated instance of <see cref="UsagiMQMiddlewarePipeline"/>.</returns>
        public UsagiMQMiddlewarePipeline Use<T>() where T : IUsagiMiddleware, new()
        {
            _middlewareComponents.Add(async (context, next) =>
            {
                var middleware = new T();
                await middleware.InvokeAsync(context, next);
            });

            return this;
        }

        /// <summary>
        /// Executes the middleware pipeline for the given message context.
        /// </summary>
        /// <param name="context">The message context containing metadata and payload.</param>
        /// <returns>A task that represents the execution of the middleware pipeline.</returns>
        public async Task ExecuteAsync(MessageContext context)
        {
            var index = 0;

            async Task Next()
            {
                if(index < _middlewareComponents.Count)
                {
                    var middleware = _middlewareComponents[index++];
                    await middleware(context, Next);
                }
            }

            await Next();
        }
    }

    /// <summary>
    /// Represents the context of a message being processed through middleware.
    /// Contains message metadata, headers, and the payload.
    /// </summary>
    public class MessageContext
    {
        /// <summary>
        /// Gets or sets the raw message payload.
        /// </summary>
        public object Message { get; set; }

        /// <summary>
        /// Gets or sets the message headers.
        /// </summary>
        public IDictionary<string, object> Headers { get; set; }

        /// <summary>
        /// Gets or sets the queue from which the message was received.
        /// </summary>
        public string QueueName { get; set; }

        /// <summary>
        /// Initializes a new instance of <see cref="MessageContext"/>.
        /// </summary>
        /// <param name="message">The message payload.</param>
        /// <param name="headers">The message headers.</param>
        /// <param name="queueName">The queue name.</param>
        public MessageContext(object message, IDictionary<string, object> headers, string queueName)
        {
            Message = message;
            Headers = headers;
            QueueName = queueName;
        }
    }
}
