using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;

namespace Uno.Extensions;

/// <summary>
/// Extensions for <see cref="IHostBuilder"/> to add serialization.
/// </summary>
public static class HostBuilderExtensions
{
	/// <summary>
	/// Adds serialization to an <see cref="IHostBuilder"/>, which can be configured using the service collection.
	/// An example of such configuration is to register <see cref="JsonSerializerOptions"/> as a singleton.
	/// </summary>
	/// <param name="hostBuilder">
	/// The <see cref="IHostBuilder"/> to add serialization to.
	/// </param>
	/// <param name="configure">
	/// A delegate to configure the <see cref="IHostBuilder"/> with serializer options.
	/// </param>
	/// <returns>
	/// The <see cref="IHostBuilder"/> with serialization added.
	/// </returns>
	[RequiresDynamicCode("Default behavior requires Reflection. Use UseSerializationResolversinstead.")]
	[RequiresUnreferencedCode("Default behavior requires Reflection. Use UseSerializationResolvers() instead.")]
	public static IHostBuilder UseSerialization(this IHostBuilder hostBuilder, Action<IServiceCollection> configure)
	{
		return hostBuilder.UseSerialization((context, builder) => configure.Invoke(builder));
	}

	/// <summary>
	/// Adds serialization to an <see cref="IHostBuilder"/>, which can be configured using the host builder context and service collection.
	/// </summary>
	/// <param name="hostBuilder">
	/// The <see cref="IHostBuilder"/> to add serialization to.
	/// </param>
	/// <param name="configure">
	/// A delegate to configure the <see cref="IHostBuilder"/> with serializer options.
	/// </param>
	/// <returns>
	/// The <see cref="IHostBuilder"/> with serialization added.
	/// </returns>
	[RequiresDynamicCode("Default behavior requires Reflection. Use UseSerializationResolversinstead.")]
	[RequiresUnreferencedCode("Default behavior requires Reflection. Use UseSerializationResolvers() instead.")]
	public static IHostBuilder UseSerialization(this IHostBuilder hostBuilder, Action<HostBuilderContext, IServiceCollection>? configure = default)
	{
		return hostBuilder
				.ConfigureServices((ctx, s) =>
				{
					_ = s.AddSystemTextJsonSerialization(ctx);
					configure?.Invoke(ctx, s);
				});
	}

	public static IHostBuilder UseSerializationResolvers(this IHostBuilder hostBuilder, Action<IServiceCollection> configure)
	{
		return hostBuilder.UseSerializationResolvers((context, builder) => configure.Invoke(builder));
	}

	public static IHostBuilder UseSerializationResolvers(this IHostBuilder hostBuilder, Action<HostBuilderContext, IServiceCollection>? configure = default, params IJsonTypeInfoResolver[] typeInfoResolvers)
	{
		return hostBuilder
			.ConfigureServices((ctx, s) =>
			{
				_ = s.AddSerialization(ctx);
				_ = s.AddSerializationJsonTypeInfoResolvers(typeInfoResolvers);
				configure?.Invoke(ctx, s);
			});
	}
}
