namespace Uno.Extensions.Hosting;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection RemoveAllIncludeImplementations<T>(this IServiceCollection collection)
    {
        return RemoveAllIncludeImplementations(collection, typeof(T));
    }

    public static IServiceCollection RemoveAllIncludeImplementations(this IServiceCollection collection, Type serviceType)
    {
        if (collection == null)
        {
            throw new ArgumentNullException(nameof(collection));
        }

        if (serviceType == null)
        {
            throw new ArgumentNullException(nameof(serviceType));
        }

        for (var i = collection.Count - 1; i >= 0; i--)
        {
            var descriptor = collection[i];
            if (descriptor.ServiceType == serviceType || descriptor.ImplementationType == serviceType)
            {
                collection.RemoveAt(i);
            }
        }

        return collection;
    }

    public static IServiceCollection RemoveWhere(this IServiceCollection collection, Func<ServiceDescriptor, bool> predicate)
    {
        if (collection == null)
        {
            throw new ArgumentNullException(nameof(collection));
        }

        if (predicate == null)
        {
            throw new ArgumentNullException(nameof(predicate));
        }

        for (var i = collection.Count - 1; i >= 0; i--)
        {
            var descriptor = collection[i];
            if (predicate(descriptor))
            {
                collection.RemoveAt(i);
            }
        }

        return collection;
    }
}
