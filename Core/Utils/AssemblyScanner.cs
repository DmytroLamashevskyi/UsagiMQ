using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UsagiMQ.API;
using UsagiMQ.Core.Attributes;

namespace UsagiMQ.Core.Utils
{
    /// <summary>
    /// Scans assemblies for RabbitMQ consumers and event processors.
    /// </summary>
    internal class AssemblyScanner
    {
        /// <summary>
        /// Finds all types with the [RabbitConsumer] attribute across all loaded assemblies.
        /// </summary>
        /// <returns>A list of consumer types.</returns>
        internal static List<Type> FindConsumers()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic) // Исключаем динамические сборки
                .SelectMany(a => a.GetTypes())
                .Where(t => t.IsClass && !t.IsAbstract && typeof(UsagiMQController).IsAssignableFrom(t))
                .Where(t => t.GetCustomAttribute<RabbitConsumerAttribute>() != null)
                .ToList();
        }

        /// <summary>
        /// Finds all methods marked with [RabbitEventProcessor] attribute across all loaded assemblies.
        /// </summary>
        /// <returns>A list of method info with attributes.</returns>
        internal static List<(MethodInfo Method, RabbitEventProcessorAttribute Attribute, Type DeclaringType)> FindEventProcessors()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic)
                .SelectMany(a => a.GetTypes())
                .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                .Where(m => m.GetCustomAttribute<RabbitEventProcessorAttribute>() != null)
                .Select(m => (Method: m, Attribute: m.GetCustomAttribute<RabbitEventProcessorAttribute>(), DeclaringType: m.DeclaringType))
                .ToList();
        }
    }
}
