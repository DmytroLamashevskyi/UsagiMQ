using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client.Events;
using RabbitMQ.Client;
using System.Reflection;
using System.Text.Json;
using UsagiMQ.API;
using UsagiMQ.Configuration;
using UsagiMQ.Core.Attributes;
using UsagiMQ.Core.Middleware;
using UsagiMQ.Core.Utils;
using UsagiMQ.Models;
using System.Text;
using System.Runtime;

namespace UsagiMQ.Core.Consumers
{
    /// <summary>
    /// Main processor that handles message routing, invocation, and reply handling.
    /// </summary>
    internal class UsagiMQProcessor
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<UsagiMQProcessor> _logger;
        private readonly UsagiMQMiddlewarePipeline _middleware;
        private readonly UsagiMQSettings _settings;
        private readonly Dictionary<string, Type> _consumerTypes = new();
        private readonly Dictionary<string, MethodInfo> _eventHandlers = new();
        private readonly Dictionary<string, IConnection> _connections = new();
        private readonly Dictionary<string, IChannel> _channels = new();
        private readonly Dictionary<string, string> _exchangeToQueue = new();
        private readonly Dictionary<string, string> _exchangeToVirtualHost = new();

        private readonly IConnection _defaultConnection;
        private readonly IChannel _defaultChannel;
        private readonly SemaphoreSlim _connectionLock = new(5);

        public UsagiMQProcessor(
            IServiceProvider serviceProvider,
            ILogger<UsagiMQProcessor> logger,
            UsagiMQMiddlewarePipeline middleware,
            UsagiMQSettings settings,
            IConnection connection)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _middleware = middleware;
            _settings = settings;
            _defaultConnection = connection;
            _defaultChannel = _defaultConnection.CreateChannelAsync().Result;
        }

        /// <summary>
        /// Initializes the RabbitMQ infrastructure and sets up consumers.
        /// </summary>
        public void Initialize()
        {
            _logger.LogInformation("[UsagiMQProcessor] Initializing UsagiMQ...");

            var consumers = AssemblyScanner.FindConsumers();
            foreach(var consumer in consumers)
            {
                RegisterConsumer(consumer);
            }

            var eventProcessors = AssemblyScanner.FindEventProcessors();
            foreach(var (method, attribute, declaringType) in eventProcessors)
            {
                RegisterEventProcessor(method, attribute, declaringType);
            }

            _logger.LogInformation($"[UsagiMQProcessor] Initialization completed. Registered {_consumerTypes.Count} consumers and {_eventHandlers.Count} event handlers.");
        }

        /// <summary>
        /// Creates necessary RabbitMQ queues and exchange bindings.
        /// </summary>
        public async Task SetupRabbitMQAsync()
        {
            _logger.LogInformation("[UsagiMQProcessor] Setting up RabbitMQ infrastructure...");

            foreach(var (queueName, consumerType) in _consumerTypes)
            {
                await _defaultChannel.QueueDeclareAsync(queueName, durable: true, exclusive: false, autoDelete: false);
                var consumer = new AsyncEventingBasicConsumer(_defaultChannel);
                consumer.ReceivedAsync += async (model, args) => await ProcessMessageAsync(queueName, args);
                await _defaultChannel.BasicConsumeAsync(queueName, autoAck: false, consumer);
                _logger.LogInformation($"[UsagiMQProcessor] Listening on queue: {queueName}");
            }

            foreach(var method in _eventHandlers.Values)
            {
                var attribute = method.GetCustomAttribute<RabbitEventProcessorAttribute>();
                if(attribute == null) continue;

                string virtualHost = attribute.VirtualHost;
                if(!_connections.ContainsKey(virtualHost))
                {
                    _connections[virtualHost] = await CreateConnectionForVirtualHost(virtualHost);
                    _channels[virtualHost] = await _connections[virtualHost].CreateChannelAsync();
                }

                var channel = _channels[virtualHost];
                _exchangeToVirtualHost[attribute.ExchangeName] = virtualHost;

                try
                {
                    await channel.ExchangeDeclarePassiveAsync(attribute.ExchangeName);
                }
                catch(Exception)
                {
                    _logger.LogError($"[UsagiMQProcessor] Exchange '{attribute.ExchangeName}' not found in VirtualHost '{virtualHost}'");
                    continue;
                }

                // 🟢 Проверяем, есть ли уже очередь для `Exchange`
                if(!_exchangeToQueue.TryGetValue(attribute.ExchangeName, out var queueName))
                {
                    queueName = $"{attribute.ExchangeName}.queue";
                    var queueAutoDelete = attribute.ExchangeType == "fanout";
                    var routingKey = attribute.ExchangeType == "fanout" ? "" : attribute.Action; // 🟢 Используем Action как routingKey

                    await channel.QueueDeclareAsync(queueName, durable: false, exclusive: true, autoDelete: queueAutoDelete);
                    await channel.QueueBindAsync(queueName, attribute.ExchangeName, routingKey);

                    _exchangeToQueue[attribute.ExchangeName] = queueName; // 🟢 Сохраняем очередь
                    _logger.LogInformation($"[UsagiMQProcessor] Created queue '{queueName}' for Exchange '{attribute.ExchangeName}'");
                }
                else
                {
                    _logger.LogInformation($"[UsagiMQProcessor] Using existing queue '{queueName}' for Exchange '{attribute.ExchangeName}'");
                }

                var eventConsumer = new AsyncEventingBasicConsumer(channel);
                eventConsumer.ReceivedAsync += async (model, args) => await ProcessEventMessageAsync(attribute.ExchangeName, args);

                await channel.BasicConsumeAsync(queue: queueName, autoAck: false, consumer: eventConsumer);
                _logger.LogInformation($"[UsagiMQProcessor] Subscribed to Exchange '{attribute.ExchangeName}' with Queue '{queueName}' in VirtualHost '{virtualHost}'");
            }

        }

        /// <summary>
        /// Создаёт канал для конкретного VirtualHost.
        /// </summary>
        private async Task<IChannel> CreateChannelForVirtualHost(string virtualHost)
        {
            var factory = new ConnectionFactory
            {
                HostName = _settings.Connection.HostName,
                Port = _settings.Connection.Port,
                UserName = _settings.Connection.UserName,
                Password = _settings.Connection.Password,
                VirtualHost = virtualHost,
                AutomaticRecoveryEnabled = _settings.Connection.AutomaticRecoveryEnabled,
                RequestedHeartbeat = TimeSpan.FromSeconds(_settings.Connection.RequestedHeartbeat),
                NetworkRecoveryInterval = TimeSpan.FromMilliseconds(_settings.Connection.NetworkRecoveryInterval)
            };

            var connection = await factory.CreateConnectionAsync();
            return await connection.CreateChannelAsync();
        }

        /// <summary>
        /// Registers a consumer type.
        /// </summary>
        private void RegisterConsumer(Type type)
        {
            var attribute = type.GetCustomAttribute<RabbitConsumerAttribute>();
            if(attribute != null)
            {
                _consumerTypes[attribute.QueueName] = type;
                _logger.LogInformation($"[UsagiMQProcessor] Registered Consumer: {type.Name} for queue '{attribute.QueueName}'");
            }
        }

        /// <summary>
        /// Registers an event processor method.
        /// </summary>
        private void RegisterEventProcessor(MethodInfo method, RabbitEventProcessorAttribute attribute, Type declaringType)
        {
            string key = $"{attribute.ExchangeName}.{attribute.Action}";
            _eventHandlers[key] = method;
            _logger.LogInformation($"[UsagiMQProcessor] Registered Event Processor: {method.Name} for Exchange '{attribute.ExchangeName}' and Action '{attribute.Action}'");
        }

        /// <summary>
        /// Обрабатывает входящее событие из RabbitMQ Exchange.
        /// </summary>
        private async Task ProcessEventMessageAsync(string exchangeName, BasicDeliverEventArgs args)
        {
            var virtualHost = GetVirtualHostByExchange(exchangeName);
            try
            {
                _logger.LogInformation($"[UsagiMQProcessor] Received event from Exchange: {exchangeName}");

                var message = JsonSerializer.Deserialize<UsagiMQEvent<dynamic>>(args.Body.ToArray());
                if(message == null)
                {
                    _logger.LogWarning($"[UsagiMQProcessor] Failed to deserialize event from Exchange: {exchangeName}");
                    return;
                }

                _logger.LogInformation($"[UsagiMQProcessor] Processing event - Target: {message.Data.Target}, Action: {message.Action}");

                string eventKey = $"{exchangeName}.{args.RoutingKey}";

                if(!_eventHandlers.TryGetValue(eventKey, out var method))
                {
                    _logger.LogWarning($"[UsagiMQProcessor] No event handler found for Exchange: {exchangeName}, Action: {message.Action}");
                    return;
                }

                using var scope = _serviceProvider.CreateScope();
                var controller = scope.ServiceProvider.GetRequiredService(method.DeclaringType) as UsagiMQController;

                if(controller == null)
                {
                    _logger.LogWarning($"[UsagiMQProcessor] Failed to resolve event processor: {method.DeclaringType?.Name}");
                    return;
                }

                InjectData(controller, args);

                // 🔹 Получаем тип единственного параметра метода
                var parameter = method.GetParameters().FirstOrDefault();
                if(parameter == null)
                {
                    _logger.LogError($"[UsagiMQProcessor] Method {method.Name} does not have any parameters.");
                    return;
                }

                // 🔹 Выполняем Middleware перед обработкой
                await _middleware.ExecuteBeforeProcessingAsync(new MessageContext(message, args.BasicProperties.Headers, exchangeName));

                // 🔹 Десериализуем `message.Data.Content` в нужный объект
                object? parameterValue;
                try
                {
                    parameterValue = JsonSerializer.Deserialize(message.Data.Content.GetRawText(), parameter.ParameterType);
                    if(parameterValue == null)
                    {
                        throw new JsonException("Deserialized object is null.");
                    }
                }
                catch(Exception ex)
                {
                    _logger.LogError(ex, $"[UsagiMQProcessor] Failed to deserialize message content to {parameter.ParameterType.Name}");
                    throw;
                }

                object? result = method.Invoke(controller, new object[] { parameterValue });

                if(result is Task taskResult)
                {
                    await taskResult;
                }

                // 🔹 Выполняем Middleware после обработки
                await _middleware.ExecuteAfterProcessingAsync(new MessageContext(message, args.BasicProperties.Headers, exchangeName));

                await _channels[virtualHost].BasicAckAsync(args.DeliveryTag, false);

                _logger.LogInformation($"[UsagiMQProcessor] Successfully processed event from Exchange: {exchangeName}, Action: {message.Action}");
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"[UsagiMQProcessor] Error processing event from Exchange: {exchangeName}");

                await _channels[virtualHost].BasicNackAsync(args.DeliveryTag, false, true);
            }
        }
        private string? GetVirtualHostByExchange(string exchangeName)
        {
            foreach(var (vHost, _) in _channels)
            {
                if(_eventHandlers.Keys.Any(k => k.StartsWith($"{exchangeName}.")))
                {
                    return vHost;
                }
            }
            return null;
        }

        private void InjectData(UsagiMQController controller, BasicDeliverEventArgs args)
        {
            // 🔹 Находим приватный метод `InjectData(BasicDeliverEventArgs args)` в `UsagiMQController`
            var injectMethod = controller.GetType().BaseType?
                .GetMethod("InjectData", BindingFlags.Instance | BindingFlags.NonPublic);

            if(injectMethod == null)
            {
                throw new InvalidOperationException("[UsagiMQProcessor] Failed to find InjectData method in UsagiMQController.");
            }

            // 🔹 Вызываем `InjectData`, передавая `BasicDeliverEventArgs`
            injectMethod.Invoke(controller, new object[] { args });
        }

        /// <summary>
        /// Processes an incoming RabbitMQ message.
        /// </summary>
        private async Task ProcessMessageAsync(string queueName, BasicDeliverEventArgs args)
        {
            try
            {
                _logger.LogInformation($"[UsagiMQProcessor] Received message from queue: {queueName}");

                var message = JsonSerializer.Deserialize<UsagiMQCommand<dynamic>>(args.Body.ToArray());
                if(message == null)
                {
                    _logger.LogWarning("[UsagiMQProcessor] Failed to deserialize message.");
                    return;
                }

                string commandKey = $"{message.Data.Target}:{message.Action}";

                if(!_consumerTypes.TryGetValue(queueName, out var consumerType))
                {
                    _logger.LogWarning($"[UsagiMQProcessor] No registered consumer for queue: {queueName}");
                    return;
                }

                using var scope = _serviceProvider.CreateScope();
                var controller = scope.ServiceProvider.GetRequiredService(consumerType) as UsagiMQController;

                if(controller == null)
                {
                    _logger.LogWarning($"[UsagiMQProcessor] Failed to resolve controller for {consumerType.Name}");
                    return;
                }

                var method = consumerType.GetMethods()
                    .FirstOrDefault(m => m.GetCustomAttribute<RabbitCommandAttribute>()?.CommandName == message.Action);

                if(method == null)
                {
                    _logger.LogWarning($"[UsagiMQProcessor] No method found for action: {message.Action}");
                    return;
                }

                // 🔹 Выполняем Middleware перед обработкой
                await _middleware.ExecuteBeforeProcessingAsync(new MessageContext(message, args.BasicProperties.Headers, queueName));

                // Вызов метода обработчика
                var result = method.Invoke(controller, new object[] { message.Data });

                if(result is Task taskResult)
                {
                    await taskResult;
                    result = taskResult.GetType().GetProperty("Result")?.GetValue(taskResult);
                }

                // 🔹 Выполняем Middleware после обработки
                await _middleware.ExecuteAfterProcessingAsync(new MessageContext(result, args.BasicProperties.Headers, queueName));

                // Отправляем ответ (если есть ReplyTo)
                if(args.BasicProperties.ReplyTo != null)
                {
                    await SendReplyAsync(args, result);
                }

                await _defaultChannel.BasicAckAsync(args.DeliveryTag, false);
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "[UsagiMQProcessor] Error processing message.");
                await _defaultChannel.BasicNackAsync(args.DeliveryTag, false, true); // requeue: true
            }
        }

        /// <summary>
        /// Sends a response message if a ReplyTo queue is provided.
        /// </summary>
        private async Task SendReplyAsync(BasicDeliverEventArgs args, object? response)
        {
            if(response == null) return;

            try
            {
                var replyTo = args.BasicProperties.ReplyTo;
                if(string.IsNullOrWhiteSpace(replyTo))
                {
                    _logger.LogWarning("[UsagiMQProcessor] ReplyTo is empty. Skipping reply.");
                    return;
                }

                var target = args.BasicProperties.Headers != null && args.BasicProperties.Headers.ContainsKey("Target")
                    ? Encoding.UTF8.GetString((byte[])args.BasicProperties.Headers["Target"])
                    : "unknown";

                var userId = args.BasicProperties.Headers != null && args.BasicProperties.Headers.ContainsKey("UserId")
                    ? Convert.ToInt32(args.BasicProperties.Headers["UserId"])
                    : 0;

                var eventData = new EventData<object>
                {
                    Target = target,
                    UserId = userId,
                    Content = response
                };

                var eventMessage = new UsagiMQEvent<object>("Reply", eventData);

                var responseBody = JsonSerializer.SerializeToUtf8Bytes(eventMessage);

                var properties = new BasicProperties();
                properties.CorrelationId = args.BasicProperties.CorrelationId;
                properties.ContentType = "application/json";

                await _defaultChannel.BasicPublishAsync("", replyTo, true, properties, responseBody);

                _logger.LogInformation($"[UsagiMQProcessor] Sent reply to queue: {replyTo} with CorrelationId: {properties.CorrelationId}");
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "[UsagiMQProcessor] Failed to send response message.");
            }
        }

        /// <summary>
        /// Creates a connection for a specific VirtualHost.
        /// </summary>
        private async Task<IConnection> CreateConnectionForVirtualHost(string virtualHost)
        {
            await _connectionLock.WaitAsync();
            try
            {
                if(_connections.ContainsKey(virtualHost))
                {
                    return _connections[virtualHost];
                }

                var factory = new ConnectionFactory
                {
                    HostName = _settings.Connection.HostName,
                    Port = _settings.Connection.Port,
                    UserName = _settings.Connection.UserName,
                    Password = _settings.Connection.Password,
                    VirtualHost = virtualHost,
                    AutomaticRecoveryEnabled = _settings.Connection.AutomaticRecoveryEnabled,
                    RequestedHeartbeat = TimeSpan.FromSeconds(_settings.Connection.RequestedHeartbeat),
                    NetworkRecoveryInterval = TimeSpan.FromMilliseconds(_settings.Connection.NetworkRecoveryInterval)
                };

                var connection = await factory.CreateConnectionAsync();
                _connections[virtualHost] = connection;
                return connection;
            }
            finally
            {
                _connectionLock.Release();
            }
        }
    }
}
