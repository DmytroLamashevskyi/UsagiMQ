using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using UsagiMQ.Configuration;
using UsagiMQ.Core.Consumers;
using UsagiMQ.Core.Middleware;

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
            services.AddOptions<UsagiMQSettings>()
                    .Configure<IConfiguration>((settings, config) =>
                    {
                        if(!string.IsNullOrEmpty(options.SettingsSection))
                        {
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
            services.AddSingleton<IConnection>(sp =>
            {
                var factory = sp.GetRequiredService<IConnectionFactory>();
                return factory.CreateConnectionAsync().GetAwaiter().GetResult();
            });

            // Register UsagiMQ Configuration Provider
            services.AddSingleton<UsagiMQConfigurationProvider>();

            // Register Middleware Pipeline
            var middlewarePipeline = new UsagiMQMiddlewarePipeline();
            options.MiddlewareConfig?.Invoke(middlewarePipeline);
            services.AddSingleton(middlewarePipeline);
            services.AddSingleton<UsagiMQProcessor>();

            services.AddSingleton<IUsagiConsumerManager, UsagiConsumerManager>();

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
        /// Configures UsagiMQ runtime services after application startup.
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <returns>The modified <see cref="IApplicationBuilder"/> for chaining.</returns>
        public static IApplicationBuilder UseUsagiMQApp(this IApplicationBuilder app)
        {
            return app;
        }
    }

    /// <summary>
    /// Provides configuration options for UsagiMQ.
    /// </summary>
    public class UsagiMQOptions
    {
        /// <summary>
        /// The section name in `appsettings.json` that contains UsagiMQ settings.
        /// </summary>
        public string? SettingsSection { get; private set; }

        /// <summary>
        /// The middleware configuration action.
        /// </summary>
        public Action<UsagiMQMiddlewarePipeline>? MiddlewareConfig { get; private set; }

        /// <summary>
        /// Indicates whether Dead Letter Exchange (DLX) is enabled.
        /// </summary>
        public bool IsEnabledDLX { get; private set; }

        /// <summary>
        /// Binds UsagiMQ settings from the specified configuration section.
        /// </summary>
        /// <param name="section">The section name in the configuration file.</param>
        /// <returns>The modified <see cref="UsagiMQOptions"/> instance.</returns>
        public UsagiMQOptions BindSettings(string section)
        {
            SettingsSection = section;
            return this;
        }

        /// <summary>
        /// Configures custom middleware for message processing.
        /// </summary>
        /// <param name="middlewareConfig">The middleware configuration action.</param>
        /// <returns>The modified <see cref="UsagiMQOptions"/> instance.</returns>
        public UsagiMQOptions UseCustomMiddleware(Action<UsagiMQMiddlewarePipeline> middlewareConfig)
        {
            MiddlewareConfig = middlewareConfig;
            return this;
        }

        /// <summary>
        /// Enables Dead Letter Exchange (DLX) functionality.
        /// </summary>
        /// <returns>The modified <see cref="UsagiMQOptions"/> instance.</returns>
        public UsagiMQOptions EnableDLX()
        {
            IsEnabledDLX = true;
            return this;
        }
    }
}
