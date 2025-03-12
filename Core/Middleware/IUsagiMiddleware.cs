using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UsagiMQ.Core.Middleware
{
    /// <summary>
    /// Defines a middleware component for UsagiMQ.
    /// Middleware can modify messages or handle exceptions before passing them to the next stage.
    /// </summary>
    public interface IUsagiMiddleware
    {
        /// <summary>
        /// Processes a message within the middleware pipeline.
        /// </summary>
        /// <param name="context">The message context.</param>
        /// <param name="next">The next middleware in the pipeline.</param>
        /// <returns>A task representing the middleware execution.</returns>
        Task InvokeAsync(MessageContext context, Func<Task> next);
    }
}
