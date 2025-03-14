namespace UsagiMQ.Core.Attributes
{
    /// <summary>
    /// Specifies that a class is a RabbitMQ consumer, binding it to a specific queue within a virtual host.
    /// This attribute is used by <see cref="UsagiConsumerManager"/> to register consumers dynamically.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class RabbitConsumerAttribute : Attribute
    {

        /// <summary>
        /// Gets the name of the queue that this consumer will listen to.
        /// </summary>
        public string QueueName { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="RabbitConsumerAttribute"/>.
        /// </summary>
        /// <param name="virtualHost">The virtual host in RabbitMQ.</param>
        /// <param name="queueName">The name of the queue to listen on.</param>
        public RabbitConsumerAttribute(string queueName)
        {
            QueueName = queueName;
        }
    }
}
