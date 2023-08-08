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
}
