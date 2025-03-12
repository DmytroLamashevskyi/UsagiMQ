using Microsoft.Extensions.Logging;
using System.Reflection;
using UsagiMQ.Core.Attributes;

namespace UsagiMQ.Core.Consumers
{
    /// <summary>
    /// Scans the assembly for RabbitMQ consumers and event processors.
    /// It registers handlers and methods based on attributes.
    /// </summary>
    internal class UsagiMQProcessor
    {
        private readonly ILogger<UsagiMQProcessor> _logger;

        /// <summary>
        /// Stores discovered consumers mapped by Target name.
        /// </summary>
        private readonly Dictionary<string, Type> _consumerTypes = new();

        /// <summary>
        /// Stores discovered event processors mapped by Action name.
        /// </summary>
        private readonly Dictionary<string, MethodInfo> _eventHandlers = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="UsagiMQProcessor"/> class.
        /// </summary>
        /// <param name="logger">Logger instance.</param>
        public UsagiMQProcessor(ILogger<UsagiMQProcessor> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Scans the current assembly and registers consumers and event processors.
        /// </summary>
        public void ScanAssembly()
        {
            _logger.LogInformation("[UsagiMQProcessor] Scanning for RabbitMQ handlers...");

            var assembly = Assembly.GetExecutingAssembly();
            var types = assembly.GetTypes();

            foreach(var type in types)
            {
                RegisterConsumer(type);
                RegisterEventProcessor(type);
            }

            _logger.LogInformation($"[UsagiMQProcessor] Scan completed. Registered {_consumerTypes.Count} consumers and {_eventHandlers.Count} event handlers.");
        }

        /// <summary>
        /// Finds and registers classes marked with `[RabbitConsumer]` attribute.
        /// </summary>
        private void RegisterConsumer(Type type)
        {
            var consumerAttribute = type.GetCustomAttribute<RabbitConsumerAttribute>();
            if(consumerAttribute != null)
            {
                _consumerTypes[consumerAttribute.QueueName] = type;
                _logger.LogInformation($"[UsagiMQProcessor] Registered Consumer: {type.Name} for queue '{consumerAttribute.QueueName}'.");
            }
        }

        /// <summary>
        /// Finds and registers methods marked with `[RabbitEventProcessor]` attribute.
        /// </summary>
        private void RegisterEventProcessor(Type type)
        {
            foreach(var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public))
            {
                var eventAttribute = method.GetCustomAttribute<RabbitEventProcessorAttribute>();
                if(eventAttribute != null)
                {
                    string key = $"{eventAttribute.ExchangeName}.{eventAttribute.Action}";
                    _eventHandlers[key] = method;

                    _logger.LogInformation($"[UsagiMQProcessor] Registered Event Processor: {method.Name} for Exchange '{eventAttribute.ExchangeName}' and Action '{eventAttribute.Action}'.");
                }
            }
        }

        /// <summary>
        /// Gets the consumer type by queue name.
        /// </summary>
        public Type? GetConsumerType(string queueName)
        {
            return _consumerTypes.TryGetValue(queueName, out var type) ? type : null;
        }

        /// <summary>
        /// Gets the event processor method by Exchange and Action name.
        /// </summary>
        public MethodInfo? GetEventHandler(string exchange, string action)
        {
            string key = $"{exchange}.{action}";
            return _eventHandlers.TryGetValue(key, out var method) ? method : null;
        }
    }
}
