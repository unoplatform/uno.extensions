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

	public static bool TryAddService(
		this IServiceCollection collection,
		ServiceDescriptor descriptor)
	{

		int count = collection.Count;
		for (int i = 0; i < count; i++)
		{
			if (collection[i].ServiceType == descriptor.ServiceType)
			{
				// Already added
				return false;
			}
		}

		collection.Add(descriptor);
		return true;
	}
}
