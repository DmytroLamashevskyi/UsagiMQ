using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Reflection;
using UsagiMQ.API;
using UsagiMQ.Configuration;
using UsagiMQ.Core.Attributes;
using UsagiMQ.Core.Consumers;
using UsagiMQ.Core.Middleware;
using UsagiMQ.Core.Utils;

namespace UsagiMQ.Extensions
{
    /// <summary>
    /// Provides extension methods for configuring UsagiMQ within an ASP.NET Core application.
    /// </summary>
    public static class UsagiMQExtensions
    {
        /// <summary>
        /// Registers UsagiMQ services into the Dependency Injection (DI) container.
        /// This method loads the configuration, initializes RabbitMQ consumers and producers,
        /// and optionally enables Dead Letter Exchange (DLX) and background processing.
        /// </summary>
        /// <param name="services">The service collection to register dependencies.</param>
        /// <param name="configure">A delegate to configure UsagiMQ options.</param>
        /// <returns>The modified <see cref="IServiceCollection"/> for chaining.</returns>
        public static IServiceCollection UseUsagiMQ(this IServiceCollection services, Action<UsagiMQOptions> configure)
        {
            var options = new UsagiMQOptions();
            configure(options);

            // Load UsagiMQ settings from configuration
            services.Configure<UsagiMQSettings>(settings =>
            {
                if(!string.IsNullOrEmpty(options.SettingsSection))
                {
                    var config = services.BuildServiceProvider().GetRequiredService<IConfiguration>();
                    config.GetSection(options.SettingsSection).Bind(settings);
                }
            });

            services.AddSingleton(sp => sp.GetRequiredService<IOptions<UsagiMQSettings>>().Value);

            // Register RabbitMQ Connection Factory
            services.AddSingleton<IConnectionFactory>(sp =>
            {
                var settings = sp.GetRequiredService<UsagiMQSettings>().Connection;
                return new ConnectionFactory
                {
                    HostName = settings.HostName,
                    Port = settings.Port,
                    UserName = settings.UserName,
                    Password = settings.Password,
                    VirtualHost = settings.VirtualHost,
                    AutomaticRecoveryEnabled = settings.AutomaticRecoveryEnabled,
                    RequestedHeartbeat = TimeSpan.FromSeconds(settings.RequestedHeartbeat),
                    NetworkRecoveryInterval = TimeSpan.FromMilliseconds(settings.NetworkRecoveryInterval)
                };
            });

            // Register RabbitMQ Connection
            services.AddSingleton(sp =>
            {
                var factory = sp.GetRequiredService<IConnectionFactory>();
                return factory.CreateConnectionAsync().Result;
            });

            // Register Middleware Pipeline
            var middlewarePipeline = new UsagiMQMiddlewarePipeline();
            options.MiddlewareConfig?.Invoke(middlewarePipeline);
            services.AddSingleton(middlewarePipeline);

            services.AddSingleton<UsagiMQProcessor>();
            services.AddRabbitConsumers();

            // Enable Dead Letter Exchange (DLX) if configured
            if(options.IsEnabledDLX)
            {
                services.Configure<UsagiMQSettings>(s => s.DLX.Enabled = true);
            }

            // Register UsagiMQ Background Service
            services.AddHostedService<UsagiMQBackgroundService>();

            return services;
        }

        /// <summary>
        /// Automatically registers all Consumers that have the [RabbitConsumer] attribute across all loaded assemblies.
        /// </summary>
        public static IServiceCollection AddRabbitConsumers(this IServiceCollection services)
        {
            AssemblyScanner.FindConsumers().ForEach(consumerType => services.AddScoped(consumerType));
            return services;
        }
    }
}
