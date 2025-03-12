using System.Text.Json.Serialization;

namespace UsagiMQ.Models
{
    /// <summary>
    /// Represents a base class for all UsagiMQ messages, including commands and events.
    /// </summary>
    /// <typeparam name="T">The type of the payload data.</typeparam>
    public abstract class UsagiMQMessage<T>
    {
        /// <summary>
        /// Unique identifier for the message.
        /// </summary>
        [JsonPropertyName("message_uuid")]
        public Guid MessageId { get; set; } = Guid.NewGuid();

        /// <summary>
        /// The action type of the message (e.g., Create, Update, Delete, Read).
        /// </summary>
        [JsonPropertyName("action")]
        public required string Action { get; set; }

        /// <summary>
        /// The timestamp when the message was created (in Unix time format).
        /// </summary>
        [JsonPropertyName("timestamp")]
        public long Timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        /// <summary>
        /// The ID of the company associated with the message.
        /// </summary>
        [JsonPropertyName("company_id")]
        public int? CompanyId { get; set; }

        /// <summary>
        /// ReplyTo queue name for responses (optional).
        /// </summary>
        [JsonPropertyName("reply_to")]
        public string? ReplyTo { get; set; }

        /// <summary>
        /// The payload data associated with the message.
        /// </summary>
        [JsonPropertyName("data")]
        public required T Data { get; set; }

        /// <summary>
        /// Initializes a new instance of <see cref="UsagiMQMessage{T}"/>.
        /// </summary>
        protected UsagiMQMessage(string action, T data)
        {
            Action = action;
            Data = data;
        }
    }
}
