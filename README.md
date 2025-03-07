# UsagiMQ - Lightweight RabbitMQ Integration for ASP.NET Core

## Overview

UsagiMQ is a RabbitMQ-based messaging framework for ASP.NET Core that simplifies communication between services. It supports both **active** (sending commands and receiving responses) and **passive** (listening to queues and processing messages) modes, while automatically handling configuration, routing, and message processing.

## Features

- **Automatic Queue and Exchange Management** – Creates and binds queues dynamically.
- **Dual-mode Operation** – Supports both **active** (REST API-based messaging) and **passive** (background message listening) modes.
- **Auto-binding via Attributes** – Developers only need to implement handlers; UsagiMQ manages subscriptions and message routing.
- **ReplyTo Support** – Automatic response handling for commands.
- **Flexible Header Mapping** – Headers from UsagiMQSettings messages are mapped to controller properties via attributes.
- **Dead Letter Exchange (DLX) Support** – Failed messages are automatically routed to DLX for later inspection.
- **Multi VirtualHost Support** – Allows connection to multiple UsagiMQSettings VirtualHosts.

---

## Installation

TBD (Once NuGet package is published)

---

## Configuration

### Dead Letter Exchange (DLX) Management

UsagiMQ supports configurable Dead Letter Exchanges (DLX). You can define DLX settings dynamically via headers or configure them globally in `appsettings.json`:

```json
"DLX": {
  "Enabled": true,
  "DefaultExchange": "usagi.dlx",
  "DefaultRoutingKey": "failed"
}
```

If a message processing fails, UsagiMQ routes it to the configured DLX automatically.

### Dependency Injection (DI) Setup

To integrate UsagiMQ into your ASP.NET Core application, register it in `Startup.cs` or `Program.cs`:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddSingleton<IUsagiMQ, UsagiMQ>();
    services.AddHostedService<UsagiMQBackgroundService>();
}
```

This ensures that UsagiMQ is properly initialized and runs as a background service for message handling.
UsagiMQ now manages VirtualHost connections dynamically. Instead of configuring VirtualHosts in `appsettings.json`, each consumer defines its VirtualHost in the attribute:

```csharp
[RabbitConsumer("vhost_name", "queue_name")]
```

UsagiMQ maintains a connection pool to manage VirtualHost connections efficiently. It supports multiple VirtualHosts, allowing developers to define different environments. Example:

````json
"UsagiMQSettings": {
  "Connections": [
    {
      "HostName": "192.168.1.115",
      "Port": 5672,
      "UserName": "warehouse_user",
      "Password": "1234abcd",
      "VirtualHost": "/warehouse"
    },
    {
      "HostName": "192.168.1.116",
      "Port": 5672,
      "UserName": "service_user",
      "Password": "abcd1234",
      "VirtualHost": "/service"
    }
  ]
}
```
Example:

```json
"UsagiMQSettings": {
  "Connections": [
    {
      "HostName": "192.168.1.115",
      "Port": 5672,
      "UserName": "warehouse_user",
      "Password": "1234abcd",
      "VirtualHost": "/warehouse",
      "RequestedHeartbeat": 60,
      "AutomaticRecoveryEnabled": true,
      "NetworkRecoveryInterval": 5000
    }
  ]
}
````

---

## Message Handling

### Message Structure

All messages in UsagiMQ follow a standardized format with metadata, filters, and pagination support. Messages are classified into `UsagiCommand<T>` for commands and `UsagiEvent<T>` for events.

### Base Consumer Class (`UsagiConsumerBase`)
UsagiMQ provides a base consumer class that automatically maps message headers to properties and manages RabbitMQ message handling.

```csharp
using System.Reflection;
using RabbitMQ.Client.Events;
using System.Text;

public abstract class UsagiConsumerBase
{
    public string? CorrelationId { get; private set; }
    public string? ReplyTo { get; private set; }
    public ulong DeliveryTag { get; private set; }
    public string Exchange { get; private set; }
    public string RoutingKey { get; private set; }

    private readonly IModel _channel;

    public void Initialize(BasicDeliverEventArgs args, IModel channel)
    {
        _channel = channel;
        DeliveryTag = args.DeliveryTag;
        Exchange = args.Exchange;
        RoutingKey = args.RoutingKey;
        ReplyTo = args.BasicProperties.ReplyTo;

        if (args.BasicProperties.Headers != null)
        {
            MapHeadersToProperties(args.BasicProperties.Headers);
        }
    }

    private void MapHeadersToProperties(IDictionary<string, object> headers)
    {
        var properties = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(prop => prop.IsDefined(typeof(FromRabbitHeaderAttribute), false));

        foreach (var prop in properties)
        {
            var attribute = prop.GetCustomAttribute<FromRabbitHeaderAttribute>();
            if (attribute != null && headers.TryGetValue(attribute.HeaderName, out var headerValue))
            {
                try
                {
                    var value = Encoding.UTF8.GetString((byte[])headerValue);
                    var convertedValue = Convert.ChangeType(value, prop.PropertyType);
                    prop.SetValue(this, convertedValue);
                }
                catch
                {
                    // Ignore conversion errors
                }
            }
        }
    }

    public void Acknowledge()
    {
        _channel.BasicAck(DeliveryTag, false);
    }

    public void Reject(bool requeue = false)
    {
        _channel.BasicNack(DeliveryTag, false, requeue);
    }
}
```

```csharp
public abstract class UsagiMessage<T>
{
    [JsonPropertyName("message_uuid")]
    public Guid MessageId { get; set; } = Guid.NewGuid();

    [JsonPropertyName("company_id")]
    public int? CompanyId { get; set; }

    [JsonPropertyName("user_id")]
    public int? UserId { get; set; }

    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    [JsonPropertyName("target")]
    public required string Target { get; set; }

    [JsonPropertyName("data")]
    public required T Data { get; set; }
}
```

### Command Message Contract (`UsagiCommand<T>`)

```csharp
public class UsagiCommand<T> : UsagiMessage<T>
{
    [JsonPropertyName("action")]
    public required string CommandType { get; set; }

    [JsonPropertyName("reply_to")]
    public string? ReplyTo { get; set; }

    [JsonPropertyName("filters")]
    public Dictionary<string, object>? Filters { get; set; }

    [JsonPropertyName("page")]
    public int Page { get; set; } = 1;

    [JsonPropertyName("page_size")]
    public int PageSize { get; set; } = 20;
}
```

### Event Message Contract (`UsagiEvent<T>`)

```csharp
public class UsagiEvent<T> : UsagiMessage<T>
{
    [JsonPropertyName("action")]
    public required string EventType { get; set; }

    [JsonPropertyName("is_success")]
    public bool IsSuccess { get; set; } = true;

    [JsonPropertyName("total_pages")]
    public int TotalPages { get; set; } = 1;
}
```

### 1. **Processing Commands (****`RabbitConsumer`****)**

To handle a command, create a class and annotate it with `[RabbitConsumer]`.

```csharp
[RabbitConsumer("warehouse", "order.created")]
public class OrOrderConsumer
    [FromRabbitHeader("X-Correlation-ID")]
    public string CorrelationId { get; set; }

    [RabbitCommand("Create")]
    public Order Handle(OrderCommand command)
    {
        return new Order { Id = command.OrderId, Status = "Created" };
    }
}
```

✅ **Automatically maps headers to properties.**\
✅ **Handles multiple commands in a single class.**\
✅ **Sends response to ****`ReplyTo`****.**

---

### 2. **Processing Events (****`RabbitEventProcessor`****)**

To subscribe to an `Exchange`, use `[RabbitEventProcessor]`.

```csharp
[RabbitEventProcessor("warehouse", "user.exchange")]
public class UserEventProcessor
{
    [FromRabbitHeader("X-Correlation-ID")]
    public string CorrelationId { get; set; }

    [RabbitCommand("UserCreated")]
    public void CreateUser(User newUser)
    {
        Db.Create(newUser);
    }
}
```

✅ **Automatically binds to Exchange.**\
✅ **Processes multiple events per class.**

---

## Sending Messages

### 1. **Sending Commands (****`UsagiMQSettingsCommand`****)**

```csharp
var response = await UsagiMQ.Send<StockStatus>("inventory.queue", new StockCheckCommand { ProductId = "12345" });
```

✅ **Automatically sends to the correct queue.**\
✅ **Handles ****`ReplyTo`**** for response processing.**

### 2. **Publishing Events (****`UsagiMQSettingsNotify`****)**

```csharp
UsagiMQ.Notify("user.exchange", new UserUpdatedEvent { UserId = "123" });
```

✅ **Publishes event to Exchange without expecting a reply.**

---

## Dead Letter Exchange (DLX)

UsagiMQ **automatically routes failed messages to DLX**. Messages in the DLX queue can be manually reprocessed by consuming from `usagi.dlx.queue` and retrying the operation or moving them back to the main queue if necessary. This ensures that transient failures do not result in permanent message loss..

- **Default DLX**: `usagi.dlx`
- **Custom DLX can be set in message headers**:
  - `x-dlx-exchange`
  - `x-dlx-routing-key`

### **Queue Configuration with DLX**

```csharp
private void ConfigureQueueWithDLX(string queueName, IDictionary<string, object> arguments)
{
    if (!arguments.ContainsKey("x-dead-letter-exchange"))
    {
        arguments["x-dead-letter-exchange"] = "usagi.dlx";
        arguments["x-dead-letter-routing-key"] = queueName + ".failed";
    }

    _channel.QueueDeclare(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: arguments);
}
```

✅ **Failed messages automatically route to ****`usagi.dlx.queue`****.**\
✅ **DLX can be overridden per message.**

---

