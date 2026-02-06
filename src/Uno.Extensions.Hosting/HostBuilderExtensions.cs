namespace Uno.Extensions;

/// <summary>
/// Extension methods for converting <see cref="IHostBuilder"/> instances to <see cref="IBuilder"/>.
/// </summary>
public static class HostBuilderExtensions
{
	/// <summary>
	/// Converts an <see cref="IHostBuilder"/> instance to the specified type 
	/// which implements <see cref="IBuilder"/>.
	/// </summary>
	/// <typeparam name="TBuilder">
	/// The builder type implementing <see cref="IBuilder"/> to convert to.
	/// </typeparam>
	/// <param name="hostBuilder">
	/// The <see cref="IHostBuilder"/> instance to convert.
	/// </param>
	/// <returns>
	/// An instance of <typeparamref name="TBuilder"/> created from the host builder.
	/// </returns>
	public static TBuilder AsBuilder<TBuilder>(this IHostBuilder hostBuilder) where TBuilder : IBuilder, new()
	{
		if (hostBuilder is TBuilder builder)
		{
			return builder;
		}

		return new TBuilder { HostBuilder = hostBuilder };
	}

	/// <summary>
	/// Checks if a specific registration key has already been registered in the host builder's properties.
	/// </summary>
	public static bool IsRegistered(this IHostBuilder builder, string registeredKey, bool newIsRegistered = true)
	{
		return builder.Properties.IsRegistered(registeredKey, newIsRegistered);
	}

	internal static bool IsRegistered(this IDictionary<object, object> properties, string registeredKey, bool newIsRegistered = true)
	{
		if (properties.TryGetValue(registeredKey, out var value) &&
			value is bool registeredValue &&
			registeredValue)
		{
			return true;
		}
		properties[registeredKey] = newIsRegistered;
		return false;
	}
}
