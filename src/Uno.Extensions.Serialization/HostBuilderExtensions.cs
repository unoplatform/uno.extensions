using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;

namespace Uno.Extensions;

/// <summary>
/// Extensions for <see cref="IHostBuilder"/> to add serialization.
/// </summary>
public static class HostBuilderExtensions
{
	internal const string RequiresDynamicCodeMessage = $"Default behavior requires Reflection. Use {nameof(UseJsonSerializationResolvers)} instead.";
	internal const string RequiresUnreferencedCodeMessage = $"Default behavior requires Reflection. Use {nameof(UseJsonSerializationResolvers)} instead.";

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
	[RequiresDynamicCode(RequiresDynamicCodeMessage)]
	[RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
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
	[RequiresDynamicCode(RequiresUnreferencedCodeMessage)]
	[RequiresUnreferencedCode(RequiresUnreferencedCodeMessage)]
	public static IHostBuilder UseSerialization(this IHostBuilder hostBuilder, Action<HostBuilderContext, IServiceCollection>? configure = default)
	{
		return hostBuilder
				.ConfigureServices((ctx, s) =>
				{
					_ = s.AddSystemTextJsonSerialization(ctx);
					configure?.Invoke(ctx, s);
				});
	}

	/// <summary>
	///   Adds JSON serialization to an <see cref="IHostBuilder"/>, which can be configured using the host builder context and service collection.
	/// </summary>
	/// <param name="hostBuilder">
	///   The <see cref="IHostBuilder"/> to add JSON serialization to.
	/// </param>
	/// <param name="typeInfoResolvers">
	///   An array of <see cref="IJsonTypeInfoResolver" /> instances to use for JSON serialization and deserialization.
	/// </param>
	/// <returns>
	///   The modified <see cref="IHostBuilder"/>.
	/// </returns>
	public static IHostBuilder UseJsonSerializationResolvers(this IHostBuilder hostBuilder, params IEnumerable<IJsonTypeInfoResolver> typeInfoResolvers)
		=> hostBuilder.UseJsonSerializationResolvers(typeInfoResolvers, (Action<HostBuilderContext, IServiceCollection>?) null);

	/// <summary>
	///   Adds JSON serialization to an <see cref="IHostBuilder"/>, which can be configured using the host builder context and service collection.
	/// </summary>
	/// <param name="hostBuilder">
	///   The <see cref="IHostBuilder"/> to add JSON serialization to.
	/// </param>
	/// <param name="typeInfoResolvers">
	///   An enumerable of <see cref="IJsonTypeInfoResolver" /> instances to use for JSON serialization and deserialization.
	/// </param>
	/// <param name="configure">
	///   An <see cref="Action{IServiceCollection}" /> to configure the <see cref="IServiceCollection" />.
	/// </param>
	/// <returns>
	///   The modified <see cref="IHostBuilder"/>.
	/// </returns>
	public static IHostBuilder UseJsonSerializationResolvers(this IHostBuilder hostBuilder, IEnumerable<IJsonTypeInfoResolver> typeInfoResolvers, Action<IServiceCollection> configure)
		=> hostBuilder.UseJsonSerializationResolvers(typeInfoResolvers, (context, builder) => configure.Invoke(builder));

	/// <summary>
	///   Adds JSON serialization to an <see cref="IHostBuilder"/>, which can be configured using the host builder context and service collection.
	/// </summary>
	/// <param name="hostBuilder">
	///   The <see cref="IHostBuilder"/> to add JSON serialization to.
	/// </param>
	/// <param name="typeInfoResolvers">
	///   An enumerable of <see cref="IJsonTypeInfoResolver" /> instances to use for JSON serialization and deserialization.
	/// </param>
	/// <param name="configure">
	///   An <see cref="Action{HostBuilderContext,IServiceCollection}" /> to configure the <see cref="IServiceCollection" />.
	/// </param>
	/// <returns>
	///   The modified <see cref="IHostBuilder"/>.
	/// </returns>
	public static IHostBuilder UseJsonSerializationResolvers(this IHostBuilder hostBuilder, IEnumerable<IJsonTypeInfoResolver> typeInfoResolvers, Action<HostBuilderContext, IServiceCollection>? configure = null)
	{
		return hostBuilder
			.ConfigureServices((ctx, s) =>
			{
				_ = s.AddJsonSerialization(ctx);
				_ = s.AddJsonSerializationTypeInfoResolvers(typeInfoResolvers);
				configure?.Invoke(ctx, s);
			});
	}
}
