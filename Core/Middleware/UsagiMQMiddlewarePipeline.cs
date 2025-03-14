namespace UsagiMQ.Core.Middleware
{
    /// <summary>
    /// Represents the middleware pipeline for processing messages in UsagiMQ.
    /// Middleware components are executed in sequence before the message reaches its consumer.
    /// </summary>
    public class UsagiMQMiddlewarePipeline
    {
        private readonly List<Func<MessageContext, Func<Task>, Task>> _beforeProcessing = new();
        private readonly List<Func<MessageContext, Func<Task>, Task>> _afterProcessing = new();

        /// <summary>
        /// Adds a middleware component that executes **before message processing**.
        /// </summary>
        public UsagiMQMiddlewarePipeline UseBeforeProcessing<T>() where T : IUsagiMiddleware, new()
        {
            _beforeProcessing.Add(async (context, next) =>
            {
                var middleware = new T();
                await middleware.InvokeAsync(context, next);
            });
            return this;
        }

        /// <summary>
        /// Adds a middleware component that executes **after message processing**.
        /// </summary>
        public UsagiMQMiddlewarePipeline UseAfterProcessing<T>() where T : IUsagiMiddleware, new()
        {
            _afterProcessing.Add(async (context, next) =>
            {
                var middleware = new T();
                await middleware.InvokeAsync(context, next);
            });
            return this;
        }

        /// <summary>
        /// Executes the **before-processing** middleware pipeline.
        /// </summary>
        public async Task ExecuteBeforeProcessingAsync(MessageContext context)
        {
            await ExecutePipelineAsync(context, _beforeProcessing);
        }

        /// <summary>
        /// Executes the **after-processing** middleware pipeline.
        /// </summary>
        public async Task ExecuteAfterProcessingAsync(MessageContext context)
        {
            await ExecutePipelineAsync(context, _afterProcessing);
        }

        /// <summary>
        /// Runs the middleware pipeline in order.
        /// </summary>
        private async Task ExecutePipelineAsync(MessageContext context, List<Func<MessageContext, Func<Task>, Task>> pipeline)
        {
            var index = 0;
            async Task Next()
            {
                if(index < pipeline.Count)
                {
                    var middleware = pipeline[index++];
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
