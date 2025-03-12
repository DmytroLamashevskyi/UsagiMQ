using Microsoft.Extensions.Configuration;

namespace UsagiMQ.Configuration
{
    /// <summary>
    /// Provides UsagiMQ settings by loading them from the application's configuration.
    /// Ensures that the settings are properly mapped and accessible throughout the application.
    /// </summary>
    public class UsagiMQConfigurationProvider
    {
        /// <summary>
        /// Holds the current UsagiMQ settings.
        /// </summary>
        public UsagiMQSettings Settings { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="UsagiMQConfigurationProvider"/>.
        /// Loads UsagiMQ settings from the provided configuration.
        /// </summary>
        /// <param name="configuration">The application's configuration instance.</param>
        public UsagiMQConfigurationProvider(IConfiguration configuration)
        {
            Settings = configuration.GetSection("UsagiMQ").Get<UsagiMQSettings>() ?? new UsagiMQSettings();
        }
    }
}
