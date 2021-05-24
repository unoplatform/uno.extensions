using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

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

        //public static IHostBuilder UseEnvironment(this IHostBuilder builder, string environmentName)
        //{
        //    return builder.ConfigureHostConfiguration(config =>
        //     {
        //         var disablereload = new Dictionary<string, string>
        //                    {
        //                        { "environment", environmentName}
        //                    };
        //         config.AddInMemoryCollection(disablereload);
        //     });
        //}
    }
}
