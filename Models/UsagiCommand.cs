using System.Text.Json.Serialization;

namespace UsagiMQ.Models
{
    /// <summary>
    /// Represents a command message sent through UsagiMQ.
    /// </summary>
    /// <typeparam name="T">The type of the payload data.</typeparam>
    public class UsagiMQCommand<T> : UsagiMQMessage<CommandData<T>>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="UsagiMQCommand{T}"/>.
        /// </summary>
        public UsagiMQCommand(string action, CommandData<T> data)
            : base(action, data) { }
    }

    /// <summary>
    /// Represents the data structure for a command message in UsagiMQ.
    /// </summary>
    /// <typeparam name="T">The type of the payload data.</typeparam>
    public class CommandData<T>
    {
        /// <summary>
        /// The target model for the command.
        /// </summary>
        [JsonPropertyName("target")]
        public required string Target { get; set; }

        /// <summary>
        /// The ID of the user initiating the command.
        /// </summary>
        [JsonPropertyName("user_id")]
        public required int UserId { get; set; }

        /// <summary>
        /// The page size for paginated responses.
        /// </summary>
        [JsonPropertyName("page_size")]
        public int PageSize { get; set; } = 10;

        /// <summary>
        /// The current page number.
        /// </summary>
        [JsonPropertyName("page")]
        public int Page { get; set; } = 1;

        /// <summary>
        /// The total pages in paginated responses.
        /// </summary>
        [JsonPropertyName("total_pages")]
        public int TotalPages { get; set; } = 1;

        /// <summary>
        /// The filters applied to the command.
        /// </summary>
        [JsonPropertyName("filters")]
        public List<FilterGroup>? Filters { get; set; }

        /// <summary>
        /// The payload of the command.
        /// </summary>
        [JsonPropertyName("content")]
        public required T Content { get; set; }
    }

    /// <summary>
    /// Represents a filter group for querying data.
    /// </summary>
    public class FilterGroup
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("inverse")]
        public bool Inverse { get; set; } = false;

        [JsonPropertyName("attribute")]
        public required string Attribute { get; set; }

        [JsonPropertyName("operator")]
        public required string Operator { get; set; }

        [JsonPropertyName("value")]
        public required object Value { get; set; }
    }
}
