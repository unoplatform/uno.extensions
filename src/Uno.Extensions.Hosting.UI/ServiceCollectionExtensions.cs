namespace Uno.Extensions;

/// <summary>
/// Extensions for manipulating elements of an IServiceCollection
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Removes all registrations of the specified type from the collection.
    /// </summary>
    /// <typeparam name="T">
    /// The type to remove from the collection.
    /// </typeparam>
    /// <param name="collection">
    /// The collection to remove the type from.
    /// </param>
    /// <returns>
    /// The collection with the specified type removed.
    /// </returns>
    public static IServiceCollection RemoveAllIncludeImplementations<T>(this IServiceCollection collection)
    {
        return RemoveAllIncludeImplementations(collection, typeof(T));
    }

    /// <summary>
    /// Removes all registrations of the specified type from the collection.
    /// </summary>
    /// <param name="collection">
    /// The collection to remove the type from.
    /// </param>
    /// <param name="serviceType">
    /// The type to remove from the collection.
    /// </param>
    /// <returns>
    /// The collection with the specified type removed.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="collection"/> or <paramref name="serviceType"/> is null.
    /// </exception>
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

    /// <summary>
    /// Removes all elements that match the conditions defined by 
    /// the specified predicate from an IServiceCollection.
    /// </summary>
    /// <param name="collection">
    /// The collection to remove elements from.
    /// </param>
    /// <param name="predicate">
    /// The <see cref="Func{T, TResult}"/> delegate that defines the conditions of the elements to remove.
    /// </param>
    /// <returns>
    /// The collection with the elements removed.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="collection"/> or <paramref name="predicate"/> is null.
    /// </exception>
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
