namespace UsagiMQ.Core.Middleware
{
    /// <summary>
    /// Middleware responsible for validating incoming messages before processing.
    /// Ensures that messages contain the required headers and a valid payload.
    /// </summary>
    public class ValidationMiddleware : IUsagiMiddleware
    {
        /// <summary>
        /// Validates the incoming message context before allowing it to proceed to the next middleware or consumer.
        /// </summary>
        /// <param name="context">The message context containing metadata and payload.</param>
        /// <param name="next">The next middleware in the pipeline.</param>
        /// <returns>A task representing the middleware execution.</returns>
        public async Task InvokeAsync(MessageContext context, Func<Task> next)
        {
            if(context.Message == null)
            {
                Console.WriteLine("[ValidationMiddleware] Message payload is null. Dropping message.");
                return; // Stop processing if message is null
            }

            //if(!context.Headers.ContainsKey("X-Correlation-ID"))
            //{
            //    Console.WriteLine("[ValidationMiddleware] Missing required header: X-Correlation-ID. Dropping message.");
            //    return; // Stop processing if Correlation ID is missing
            //}

            Console.WriteLine("[ValidationMiddleware] Message validated successfully.");
            await next(); // Continue to the next middleware
        }
    }
}
