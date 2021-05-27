using System;
using Microsoft.Extensions.DependencyInjection;

namespace Uno.Extensions.Hosting
{
    public static class ServiceCollectionExtensions
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
