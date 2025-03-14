using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace UsagiMQ.Core.Utils
{
    /// <summary>
    /// Provides helper methods for serialization, deserialization, and handling message properties in UsagiMQ.
    /// </summary>
    internal static class UsagiMQHelper
    {
        /// <summary>
        /// Serializes an object to a UTF-8 encoded JSON byte array.
        /// </summary>
        /// <typeparam name="T">The type of the object.</typeparam>
        /// <param name="data">The object to serialize.</param>
        /// <returns>A byte array representing the JSON-encoded object.</returns>
        public static byte[] SerializeMessage<T>(T data)
        {
            return Encoding.UTF8.GetBytes(JsonSerializer.Serialize(data));
        }

        /// <summary>
        /// Deserializes a UTF-8 encoded JSON byte array into an object of type T.
        /// </summary>
        /// <typeparam name="T">The target object type.</typeparam>
        /// <param name="body">The byte array containing JSON-encoded data.</param>
        /// <returns>An instance of type T.</returns>
        public static T DeserializeMessage<T>(byte[] body)
        {
            return JsonSerializer.Deserialize<T>(Encoding.UTF8.GetString(body))!;
        }

        /// <summary>
        /// Sets RabbitMQ headers for a message.
        /// </summary>
        /// <param name="properties">The message properties.</param>
        /// <param name="headers">A dictionary of headers to set.</param>
        public static void SetHeaders(IBasicProperties properties, Dictionary<string, object> headers)
        {
            if(properties.Headers == null)
                properties.Headers = new Dictionary<string, object>();

            foreach(var header in headers)
            {
                properties.Headers[header.Key] = header.Value;
            }
        }

        /// <summary>
        /// Конвертирует `dynamic content` в нужный тип `parameterType`.
        /// </summary>
        internal static object ConvertToType(dynamic content, Type parameterType)
        {
            try
            {
                if(content == null)
                    return Activator.CreateInstance(parameterType) ?? throw new InvalidOperationException($"Cannot create instance of {parameterType}");

                string json = JsonSerializer.Serialize(content);
                return JsonSerializer.Deserialize(json, parameterType) ?? throw new InvalidOperationException($"Failed to deserialize content to {parameterType}");
            }
            catch(Exception ex)
            {
                throw new InvalidOperationException($"Error converting content to {parameterType}: {ex.Message}", ex);
            }
        }
    }
}
