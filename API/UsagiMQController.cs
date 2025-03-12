using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using UsagiMQ.Core.Utils;
using UsagiMQ.Models;

namespace UsagiMQ.API
{
    public abstract class UsagiMQController
    {
        private IChannel _channel { get; set; }

        /// <summary>
        /// The RabbitMQ delivery arguments containing message metadata.
        /// </summary>
        public BasicDeliverEventArgs DeliveryArgs { get; private set; } = null!;

        /// <summary>
        /// The RabbitMQ message properties.
        /// </summary>
        public IReadOnlyBasicProperties Properties => DeliveryArgs.BasicProperties;

        /// <summary>
        /// The ReplyTo queue for sending responses.
        /// </summary>
        public string? ReplyTo => Properties.ReplyTo;

        /// <summary>
        /// The message correlation ID.
        /// </summary>
        public string? CorrelationId => Properties.CorrelationId;

        /// <summary>
        /// The headers of the RabbitMQ message.
        /// </summary>
        public IDictionary<string, object> Headers => Properties.Headers ?? new Dictionary<string, object>();

        /// <summary>
        /// Extracts and deserializes the full UsagiMQ message from the RabbitMQ body.
        /// </summary>
        /// <typeparam name="T">The expected message payload type.</typeparam>
        /// <returns>The deserialized message.</returns>
        public UsagiMQMessage<T> GetMessage<T>()
        {
            return UsagiMQHelper.DeserializeMessage<UsagiMQMessage<T>>(DeliveryArgs.Body.ToArray());
        }

        /// <summary>
        /// Sends a negative acknowledgment (NACK) to RabbitMQ, indicating that the message should not be re-queued.
        /// </summary>
        public async Task NackAsync()
        {
            if(_channel == null)
            {
                throw new InvalidOperationException("Channel is not initialized.");
            }

            await _channel.BasicNackAsync(deliveryTag: DeliveryArgs.DeliveryTag, multiple: false, requeue: false);
        }

        /// <summary>
        /// Sends a positive acknowledgment (ACK) to RabbitMQ, confirming successful message processing.
        /// </summary>
        public async Task AckAsync()
        {
            if(_channel == null)
            {
                throw new InvalidOperationException("Channel is not initialized.");
            }

            await _channel.BasicAckAsync(deliveryTag: DeliveryArgs.DeliveryTag, multiple: false);
        }
    }
}
