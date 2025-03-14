using UsagiMQ.Core.Middleware;

namespace UsagiMQ.Extensions
{
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
