namespace UsagiMQ.Core.Utils
{
    /// <summary>
    /// Defines constant values used throughout the UsagiMQ framework.
    /// </summary>
    internal static class UsagiMQConstants
    {
        /// <summary>
        /// Default exchange name for UsagiMQ events.
        /// </summary>
        public const string DefaultExchange = "usagi.exchange";

        /// <summary>
        /// Default dead-letter exchange (DLX) for handling failed messages.
        /// </summary>
        public const string DeadLetterExchange = "usagi.dlx";

        /// <summary>
        /// Default queue name for dead-letter messages.
        /// </summary>
        public const string DeadLetterQueue = "usagi.dlx.queue";

        /// <summary>
        /// Header key used for correlation ID tracking.
        /// </summary>
        public const string CorrelationIdHeader = "X-Correlation-ID";

        /// <summary>
        /// Header key for specifying the type of the message.
        /// </summary>
        public const string MessageTypeHeader = "X-Message-Type";

        /// <summary>
        /// Header key for reply-to response queue.
        /// </summary>
        public const string ReplyToHeader = "ReplyTo";

        /// <summary>
        /// Header key for indicating whether a message is a retry.
        /// </summary>
        public const string RetryHeader = "X-Retry-Count";

        /// <summary>
        /// The maximum number of times a message can be retried before moving to DLX.
        /// </summary>
        public const int MaxRetryCount = 5;

        /// <summary>
        /// The routing key for dead-letter messages.
        /// </summary>
        public const string DeadLetterRoutingKey = "usagi.dlx.routing";

        /// <summary>
        /// The default time-to-live (TTL) for messages in DLX before reprocessing.
        /// </summary>
        public const int DefaultMessageTTL = 60000; // 60 seconds

        /// <summary>
        /// Exchange types used in RabbitMQ.
        /// </summary>
        public static class ExchangeTypes
        {
            public const string Direct = "direct";
            public const string Fanout = "fanout";
            public const string Topic = "topic";
        }
    }
}
