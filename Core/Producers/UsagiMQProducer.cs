using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Collections.Concurrent;
using UsagiMQ.Core.Utils;

namespace UsagiMQ.Core.Producers
{
    /// <summary>
    /// Implements the UsagiMQ producer for sending commands (RPC) and publishing events (Pub/Sub).
    /// </summary>
    public class UsagiMQProducer : IUsagiMQProducer, IDisposable
    {
        private readonly IConnection _connection;
        private readonly string _virtualHost;
        private readonly Lazy<IChannel> _channel;

        /// <summary>
        /// Initializes a new instance of <see cref="UsagiMQProducer"/>.
        /// </summary>
        /// <param name="connection">The RabbitMQ connection instance.</param>
        /// <param name="virtualHost">The VirtualHost for this producer.</param>
        public UsagiMQProducer(IConnection connection, string virtualHost)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _virtualHost = virtualHost ?? throw new ArgumentNullException(nameof(virtualHost));

            // Отложенное создание канала (Lazy)
            _channel = new Lazy<IChannel>(() =>
            {
                var model = _connection.CreateChannelAsync().GetAwaiter().GetResult();
                model.ExchangeDeclareAsync(UsagiMQConstants.DefaultExchange, UsagiMQConstants.ExchangeTypes.Fanout, durable: true).GetAwaiter().GetResult();
                return model;
            });
        }

        /// <inheritdoc/>
        public async Task<TResponse> SendAsync<TResponse, TCommand>(
            string queue,
            TCommand command,
            int timeoutMs = 5000,
            Dictionary<string, object>? headers = null)
        {
            var channel = _channel.Value;
            await channel.BasicQosAsync(0, 1, false);

            var replyQueue = channel.QueueDeclareAsync().GetAwaiter().GetResult().QueueName;
            var correlationId = Guid.NewGuid().ToString();

            var properties = new BasicProperties();
            properties.CorrelationId = correlationId;
            properties.ReplyTo = replyQueue;
            UsagiMQHelper.SetHeaders(properties, headers ?? new Dictionary<string, object>());

            var messageBody = UsagiMQHelper.SerializeMessage(command);
            await channel.BasicPublishAsync(exchange: "", routingKey: queue, mandatory: true, basicProperties: properties, body: messageBody);

            var taskCompletionSource = new TaskCompletionSource<TResponse>();
            var consumer = new AsyncEventingBasicConsumer(channel);

            consumer.ReceivedAsync += async (model, eventArgs) =>
            {
                try
                {
                    if(eventArgs.BasicProperties.CorrelationId == correlationId)
                    {
                        var response = UsagiMQHelper.DeserializeMessage<TResponse>(eventArgs.Body.ToArray());
                        taskCompletionSource.SetResult(response);
                    }
                }
                catch(Exception ex)
                {
                    taskCompletionSource.SetException(ex);
                }

                await Task.CompletedTask;
            };

            await channel.BasicConsumeAsync(queue: replyQueue, autoAck: true, consumer: consumer);

            var completedTask = await Task.WhenAny(taskCompletionSource.Task, Task.Delay(timeoutMs));
            if(completedTask == taskCompletionSource.Task)
            {
                return await taskCompletionSource.Task;
            }
            else
            {
                throw new TimeoutException($"[UsagiMQProducer] Timeout waiting for response from {queue}");
            }
        }

        /// <inheritdoc/>
        public async Task NotifyAsync<TEvent>(
            string exchange,
            string routingKey,
            TEvent eventMessage,
            Dictionary<string, object>? headers = null,
            int? delayMs = null)
        {
            var channel = _channel.Value;
            var messageBody = UsagiMQHelper.SerializeMessage(eventMessage);

            var properties = new BasicProperties();
            properties.Persistent = true;
            UsagiMQHelper.SetHeaders(properties, headers ?? new Dictionary<string, object>());

            if(delayMs.HasValue)
            {
                properties.Headers ??= new Dictionary<string, object>();
                properties.Headers["x-delay"] = delayMs.Value;
            }

            await channel.BasicPublishAsync(exchange: exchange, routingKey: routingKey, mandatory: false, basicProperties: properties, body: messageBody);
            await Task.CompletedTask;
        }

        /// <summary>
        /// Disposes the RabbitMQ channel.
        /// </summary>
        public void Dispose()
        {
            if(_channel.IsValueCreated)
            {
                _channel.Value.Dispose();
            }
        }
    }
}
