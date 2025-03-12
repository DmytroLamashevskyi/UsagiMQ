using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Reflection;
using System.Text;
using UsagiMQ.API;
using UsagiMQ.Configuration;
using UsagiMQ.Core.Attributes;
using UsagiMQ.Core.Utils;
using UsagiMQ.Models;

namespace UsagiMQ.Core.Consumers
{
    /// <summary>
    /// Manages the lifecycle of UsagiMQ consumers, including startup, shutdown, and monitoring.
    /// </summary>
    public class UsagiConsumerManager : IUsagiConsumerManager
    {
        private readonly UsagiMQSettings _settings;
        private readonly ILogger<UsagiConsumerManager> _logger;
        private readonly IConnection _connection;
        private readonly IChannel _channel;
        private readonly Dictionary<string, UsagiMQController> _consumers = new();
        private readonly Dictionary<string, MethodInfo> _commandHandlers = new();
        private readonly Dictionary<string, MethodInfo> _eventHandlers = new();
        private bool _isRunning = false;

        public UsagiConsumerManager(
            IConnection connection,
            ILogger<UsagiConsumerManager> logger,
            IOptions<UsagiMQSettings> settings)
        {
            _connection = connection;
            _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();
            _logger = logger;
            _settings = settings.Value;
        }

        /// <summary>
        /// Scans the assembly and registers all consumers and event processors.
        /// </summary>
        public Task RegisterConsumersAsync()
        {
            _logger.LogInformation("[UsagiConsumerManager] Registering consumers...");

            var consumerTypes = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => t.GetCustomAttribute<RabbitConsumerAttribute>() != null);

            foreach(var type in consumerTypes)
            {
                var attribute = type.GetCustomAttribute<RabbitConsumerAttribute>();
                if(attribute == null) continue;

                var consumerInstance = Activator.CreateInstance(type) as UsagiMQController;
                if(consumerInstance != null)
                {
                    _consumers[attribute.QueueName] = consumerInstance;
                    _logger.LogInformation($"[UsagiConsumerManager] Registered consumer: {type.Name} for queue: {attribute.QueueName}");

                    // Register all methods annotated with [RabbitCommand]
                    foreach(var method in type.GetMethods())
                    {
                        var commandAttr = method.GetCustomAttribute<RabbitCommandAttribute>();
                        if(commandAttr != null)
                        {
                            string commandKey = $"{commandAttr.Target}:{commandAttr.CommandName}";
                            _commandHandlers[commandKey] = method;
                            _logger.LogInformation($"[UsagiConsumerManager] Registered command handler: {method.Name} for target: {commandAttr.Target}, action: {commandAttr.CommandName}");
                        }
                    }
                }
            }

            _logger.LogInformation("[UsagiConsumerManager] Consumer registration completed.");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Scans the assembly and registers event processors.
        /// </summary>
        private Task RegisterEventProcessorsAsync()
        {
            _logger.LogInformation("[UsagiConsumerManager] Registering event processors...");

            var eventHandlers = Assembly.GetExecutingAssembly()
                .GetTypes()
                .SelectMany(t => t.GetMethods())
                .Where(m => m.GetCustomAttribute<RabbitEventProcessorAttribute>() != null);

            foreach(var method in eventHandlers)
            {
                var attribute = method.GetCustomAttribute<RabbitEventProcessorAttribute>();
                if(attribute == null) continue;

                string eventKey = $"{attribute.ExchangeName}:{attribute.Action}";

                if(_eventHandlers.ContainsKey(eventKey))
                {
                    _logger.LogWarning($"[UsagiConsumerManager] Event handler for {attribute.Action} in {attribute.ExchangeName} already exists.");
                    continue;
                }

                _eventHandlers[eventKey] = method;
                _logger.LogInformation($"[UsagiConsumerManager] Registered event processor: {method.Name} for Exchange: {attribute.ExchangeName}, Action: {attribute.Action}");
            }

            _logger.LogInformation("[UsagiConsumerManager] Event processor registration completed.");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Подписывает очередь на Exchange и начинает обработку событий.
        /// </summary>
        private async Task SubscribeToEventExchangeAsync(string exchangeName, string exchangeType)
        {
            _logger.LogInformation($"[UsagiConsumerManager] Subscribing to Exchange: {exchangeName} ({exchangeType})");

            await _channel.ExchangeDeclareAsync(exchange: exchangeName, type: exchangeType, durable: true);
            var queueName = await _channel.QueueDeclareAsync("", durable: false, exclusive: true, autoDelete: true);
            await _channel.QueueBindAsync(queueName, exchangeName, "");

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += async (_, args) => await ProcessEventMessageAsync(exchangeName, args);
            await _channel.BasicConsumeAsync(queue: queueName, autoAck: true, consumer: consumer);

            _logger.LogInformation($"[UsagiConsumerManager] Listening on Exchange: {exchangeName} with queue: {queueName}");
        }

        public Task<bool> IsRunningAsync() => Task.FromResult(_isRunning);

        public async Task StartListeningAsync(CancellationToken stoppingToken)
        {
            if(_isRunning)
            {
                _logger.LogWarning("[UsagiConsumerManager] Consumers are already running.");
                return;
            }

            _logger.LogInformation("[UsagiConsumerManager] Starting message listening...");

            await RegisterConsumersAsync();
            await RegisterEventProcessorsAsync();

            foreach(var (queueName, consumer) in _consumers)
            {
                stoppingToken.ThrowIfCancellationRequested();

                await _channel.QueueDeclareAsync(queue: queueName, durable: true, exclusive: false, autoDelete: false);

                var asyncConsumer = new AsyncEventingBasicConsumer(_channel);
                asyncConsumer.ReceivedAsync += async (_, args) => await ProcessMessageAsync(queueName, args);

                await _channel.BasicConsumeAsync(queue: queueName, autoAck: false, consumer: asyncConsumer);

                _logger.LogInformation($"[UsagiConsumerManager] Listening on queue: {queueName}");
            }

            _isRunning = true;
            _logger.LogInformation("[UsagiConsumerManager] All consumers started successfully.");
        }

        public async Task StopListeningAsync()
        {
            if(!_isRunning)
            {
                _logger.LogWarning("[UsagiConsumerManager] Consumers are not running.");
                return;
            }

            _logger.LogInformation("[UsagiConsumerManager] Stopping consumers...");

            try
            {
                if(_channel != null)
                {
                    await _channel.CloseAsync();
                    _logger.LogInformation("[UsagiConsumerManager] RabbitMQ channel closed.");
                }

                if(_connection != null)
                {
                    await _connection.CloseAsync();
                    _logger.LogInformation("[UsagiConsumerManager] RabbitMQ connection closed.");
                }

                _isRunning = false;
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "[UsagiConsumerManager] Error while stopping consumers.");
            }

            _logger.LogInformation("[UsagiConsumerManager] Consumers stopped.");
        }

        public async Task RestartListeningAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("[UsagiConsumerManager] Restarting consumers...");

            await StopListeningAsync();
            await RegisterConsumersAsync();
            await StartListeningAsync(stoppingToken);

            _logger.LogInformation("[UsagiConsumerManager] Consumers restarted successfully.");
        }

        /// <summary>
        /// Обрабатывает входящее сообщение от Exchange.
        /// </summary>
        private async Task ProcessEventMessageAsync(string exchangeName, BasicDeliverEventArgs args)
        {
            try
            {
                _logger.LogInformation($"[UsagiConsumerManager] Received event from Exchange: {exchangeName}");

                var message = UsagiMQHelper.DeserializeMessage<UsagiMQEvent<dynamic>>(args.Body.ToArray());

                if(message == null)
                {
                    _logger.LogWarning($"[UsagiConsumerManager] Failed to deserialize event from Exchange: {exchangeName}");
                    return;
                }

                _logger.LogInformation($"[UsagiConsumerManager] Processing event - Target: {message.Data.Target}, Action: {message.Action}");

                string eventKey = $"{exchangeName}:{message.Action}";

                if(!_commandHandlers.TryGetValue(eventKey, out var method))
                {
                    _logger.LogWarning($"[UsagiConsumerManager] No event handler found for Exchange: {exchangeName}, Action: {message.Action}");
                    return;
                }

                var parameters = method.GetParameters();
                var parameterValues = parameters.Select(param =>
                    param.ParameterType == typeof(BasicDeliverEventArgs) ? args :
                    UsagiMQHelper.ConvertToType(message.Data.Content, param.ParameterType)).ToArray();

                object? result = method.Invoke(null, parameterValues);

                if(result is Task taskResult)
                {
                    await taskResult;
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"[UsagiConsumerManager] Error processing event from Exchange: {exchangeName}");
            }
        }

        private async Task ProcessMessageAsync(string queueName, BasicDeliverEventArgs args)
        {
            try
            {
                _logger.LogInformation($"[UsagiConsumerManager] Received message from queue: {queueName}");

                var message = UsagiMQHelper.DeserializeMessage<UsagiMQCommand<dynamic>>(args.Body.ToArray());

                if(message == null)
                {
                    _logger.LogWarning($"[UsagiConsumerManager] Failed to deserialize message from queue: {queueName}");
                    await SendToDLXAsync(args);
                    return;
                }

                _logger.LogInformation($"[UsagiConsumerManager] Processing command - Target: {message.Data.Target}, Action: {message.Action}");

                string commandKey = $"{message.Data.Target}:{message.Action}";

                if(!_commandHandlers.TryGetValue(commandKey, out var method))
                {
                    _logger.LogWarning($"[UsagiConsumerManager] No command handler found for target: {message.Data.Target}, action: {message.Action}");
                    await SendToDLXAsync(args);
                    return;
                }

                var consumer = _consumers[queueName];
                var parameters = method.GetParameters();
                var parameterValues = parameters.Select(param =>
                    param.ParameterType == typeof(BasicDeliverEventArgs) ? args :
                    UsagiMQHelper.ConvertToType(message.Data.Content, param.ParameterType)).ToArray();

                object? result = method.Invoke(consumer, parameterValues);

                if(result is Task taskResult)
                {
                    await taskResult;
                    result = taskResult.GetType().GetProperty("Result")?.GetValue(taskResult);
                }

                // Извлекаем ReplyTo из заголовков, если он там присутствует
                string? replyTo = args.BasicProperties.Headers?.ContainsKey("ReplyTo") == true
                    ? Encoding.UTF8.GetString((byte[])args.BasicProperties.Headers["ReplyTo"])
                    : null;

                if(!string.IsNullOrEmpty(replyTo))
                {
                    await SendReplyAsync(args, result);
                }

                await consumer.AckAsync();
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"[UsagiConsumerManager] Error processing message from queue: {queueName}");
                await SendToDLXAsync(args);
            }
        }

        private async Task SendReplyAsync(BasicDeliverEventArgs args, object? response)
        {
            try
            {
                // 1️⃣ Получаем `ReplyTo` из заголовков
                if(!args.BasicProperties.Headers.TryGetValue("ReplyTo", out var replyToObj) || replyToObj is not byte[] replyToBytes)
                {
                    _logger.LogWarning("[UsagiConsumerManager] No ReplyTo header found. Response will not be sent.");
                    return;
                }

                string replyTo = System.Text.Encoding.UTF8.GetString(replyToBytes);
                if(string.IsNullOrWhiteSpace(replyTo))
                {
                    _logger.LogWarning("[UsagiConsumerManager] ReplyTo header is empty. Response will not be sent.");
                    return;
                }

                _logger.LogInformation($"[UsagiConsumerManager] Sending response to ReplyTo queue: {replyTo}");

                // 2️⃣ Сериализуем объект в JSON
                var responseBody = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(response);

                // 3️⃣ Настраиваем заголовки
                var properties = new BasicProperties();
                properties.CorrelationId = args.BasicProperties.CorrelationId ?? Guid.NewGuid().ToString();
                properties.ContentType = "application/json";

                // 4️⃣ Отправляем ответ в очередь `ReplyTo`
                await _channel.BasicPublishAsync(
                    exchange: "",
                    routingKey: replyTo,
                    mandatory: true,
                    basicProperties: properties,
                    body: responseBody
                );

                _logger.LogInformation($"[UsagiConsumerManager] Response sent to {replyTo} with CorrelationId: {properties.CorrelationId}");
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "[UsagiConsumerManager] Failed to send response message.");
            }
        }

        private async Task SendToDLXAsync(BasicDeliverEventArgs args)
        {
            try
            {
                if(!_settings.DLX.Enabled)
                {
                    _logger.LogWarning("[UsagiConsumerManager] DLX is disabled. The failed message will not be stored.");
                    return;
                }

                _logger.LogWarning($"[UsagiConsumerManager] Sending message to DLX: {_settings.DLX.Exchange}");

                await _channel.ExchangeDeclareAsync(_settings.DLX.Exchange, ExchangeType.Direct, durable: true);
                await _channel.QueueDeclareAsync(_settings.DLX.Queue, durable: true, exclusive: false, autoDelete: false);
                await _channel.QueueBindAsync(_settings.DLX.Queue, _settings.DLX.Exchange, _settings.DLX.RoutingKey);

                var properties = new BasicProperties(args.BasicProperties);
                await _channel.BasicPublishAsync(_settings.DLX.Exchange, _settings.DLX.RoutingKey, true, properties, args.Body);

                _logger.LogInformation($"[UsagiConsumerManager] Message moved to DLX queue: {_settings.DLX.Queue}");
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "[UsagiConsumerManager] Failed to send message to DLX.");
            }
        }
    }
}
