namespace Uno.Extensions.Maui.Internals;

internal static class ServiceCollectionExtensions
{
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
