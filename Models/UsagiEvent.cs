using System.Text.Json.Serialization;

namespace UsagiMQ.Models
{
    /// <summary>
    /// Represents an event message sent through UsagiMQ.
    /// </summary>
    /// <typeparam name="T">The type of the payload data.</typeparam>
    public class UsagiMQEvent<T> : UsagiMQMessage<EventData<T>>
    {
        /// <summary>
        /// Indicates whether the event processing was successful.
        /// </summary>
        [JsonPropertyName("is_success")]
        public bool IsSuccess { get; set; } = true;

        /// <summary>
        /// Initializes a new instance of <see cref="UsagiMQEvent{T}"/>.
        /// </summary>
        public UsagiMQEvent(string action, EventData<T> data)
            : base(action, data) { }
    }

    /// <summary>
    /// Represents the data structure for an event message in UsagiMQ.
    /// </summary>
    /// <typeparam name="T">The type of the payload data.</typeparam>
    public class EventData<T>
    {
        /// <summary>
        /// The target model for the event.
        /// </summary>
        [JsonPropertyName("target")]
        public required string Target { get; set; }

        /// <summary>
        /// The ID of the user associated with the event.
        /// </summary>
        [JsonPropertyName("user_id")]
        public required int UserId { get; set; }

        /// <summary>
        /// The payload of the event.
        /// </summary>
        [JsonPropertyName("content")]
        public required T Content { get; set; }
    }
}
