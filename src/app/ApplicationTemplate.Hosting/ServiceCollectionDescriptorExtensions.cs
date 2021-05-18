using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Logging.Debug;
using Microsoft.Extensions.Logging.EventLog;

namespace Microsoft.Extensions.DependencyInjection.Extensions
{
    public static class ServiceCollectionDescriptorExtensions
    {
        public static IServiceCollection RemoveAllIncludeImplementations<T>(this IServiceCollection collection)
        {
            return RemoveAllIncludeImplementations(collection, typeof(T));
        }

        public static IServiceCollection RemoveAllIncludeImplementations(this IServiceCollection collection, Type serviceType)
        {
            if (serviceType == null)
            {
                throw new ArgumentNullException(nameof(serviceType));
            }

            for (int i = collection.Count - 1; i >= 0; i--)
            {
                ServiceDescriptor? descriptor = collection[i];
                if (descriptor.ServiceType == serviceType || descriptor.ImplementationType == serviceType)
                {
                    collection.RemoveAt(i);
                }
            }

            return collection;
        }
    }
}
