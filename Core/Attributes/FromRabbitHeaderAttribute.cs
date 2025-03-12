namespace UsagiMQ.Core.Attributes
{
    /// <summary>
    /// Specifies that a method parameter should be populated with a value from a RabbitMQ message header.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class FromRabbitHeaderAttribute : Attribute
    {
        /// <summary>
        /// Gets the header name to be mapped to the parameter.
        /// </summary>
        public string HeaderName { get; }

        /// <summary>
        /// Initializes a new instance of <see cref="FromRabbitHeaderAttribute"/>.
        /// </summary>
        /// <param name="headerName">The name of the RabbitMQ message header.</param>
        public FromRabbitHeaderAttribute(string headerName)
        {
            HeaderName = headerName;
        }
    }
}
