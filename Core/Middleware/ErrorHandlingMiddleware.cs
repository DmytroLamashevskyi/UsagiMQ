namespace UsagiMQ.Core.Middleware
{
    /// <summary>
    /// Middleware responsible for catching and handling exceptions that occur during message processing.
    /// Ensures that errors are logged instead of breaking the processing pipeline.
    /// </summary>
    public class ErrorHandlingMiddleware : IUsagiMiddleware
    {
        /// <summary>
        /// Handles any exceptions that occur while processing a message.
        /// Logs the error and prevents message loss due to unhandled exceptions.
        /// </summary>
        /// <param name="context">The message context containing metadata and payload.</param>
        /// <param name="next">The next middleware in the pipeline.</param>
        /// <returns>A task representing the middleware execution.</returns>
        public async Task InvokeAsync(MessageContext context, Func<Task> next)
        {
            try
            {
                await next(); // Continue processing the message
            }
            catch(Exception ex)
            {
                Console.WriteLine($"[ErrorHandlingMiddleware] Error processing message from {context.QueueName}: {ex.Message}");
                Console.WriteLine(ex.StackTrace);

                // TODO: Implement proper logging (e.g., log to a file, monitoring system, or DLX queue)
            }
        }
    }
}
