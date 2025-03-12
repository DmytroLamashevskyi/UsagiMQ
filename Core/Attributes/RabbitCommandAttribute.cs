namespace UsagiMQ.Core.Attributes
{
    /// <summary>
    /// Specifies that a method should handle a specific RabbitMQ command.
    /// This attribute is used to map incoming messages to their corresponding handlers.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class RabbitCommandAttribute : Attribute
    {
        /// <summary>
        /// Gets the command name that this handler method processes.
        /// </summary>
        public string CommandName { get; }

        /// <summary>
        /// Gets the command name that this handler method processes.
        /// </summary>
        public string Target { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="RabbitCommandAttribute"/>.
        /// </summary>
        /// <param name="commandName">The name of the command this handler processes.</param>
        public RabbitCommandAttribute(string commandName,string target)
        {
            CommandName = commandName;
            Target = target;
        }
    }
}
