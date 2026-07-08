using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;

using Microsoft.Extensions.Options;

namespace Uno.Extensions;

/// <summary>
/// This class is used for serialization configuration.
/// - Configures the serializers.
/// </summary>
public static class ServiceCollectionExtensions
{
	/// <summary>
	///   Adds the serialization services to the <see cref="IServiceCollection"/>.
	/// </summary>
	/// <param name="services">
	///   The <see cref="IServiceCollection"/> to add JSON Serialization to.
	/// </param>
	/// <param name="context">The <see cref="HostBuilderContext"/> to use when adding services.</param>
	/// <param name="typeInfoResolvers">
	///   An enumerable of <see cref="IJsonTypeInfoResolver" /> instances to use for JSON serialization and deserialization.
	/// </param>
	/// <returns>
	///   The modified <see cref="IServiceCollection" />.
	/// </returns>
	public static IServiceCollection AddJsonSerialization(
		this IServiceCollection services,
		HostBuilderContext context,
		params IEnumerable<IJsonTypeInfoResolver> typeInfoResolvers)
	{
		if (context.IsRegistered(nameof(AddJsonSerialization)))
		{
			return services;
		}
		return services
			.AddSingleton(sp => sp.GetJsonSerializationOptions())
			.AddSingleton<SystemTextJsonSerializer>()
			.AddSingleton<ISerializer>(services => services.GetRequiredService<SystemTextJsonSerializer>())
			.AddSingleton(typeof(ISerializer<>), typeof(SystemTextJsonGeneratedSerializer<>))
			.AddJsonTypeInfo(typeInfoResolvers);
	}

	internal static JsonSerializerOptions GetJsonSerializationOptions(this IServiceProvider services)
		=> services.GetService<IOptions<JsonSerializationOptions>>()?.Value?.SerializerOptions ??
			JsonSerializationOptions.DefaultSerializerOptions;


	/// <summary>
	/// Adds the serialization services to the <see cref="IServiceCollection"/>.
	/// </summary>
	/// <param name="services">Service collection.</param>
	/// <param name="context">The <see cref="HostBuilderContext"/> to use when adding services</param>
	/// <returns><see cref="IServiceCollection"/>.</returns>
	/// <remarks>
	///   Consider using <see cref="AddJsonSerialization" /> and <see cref="AddJsonTypeInfo" />
	///   for use in trimming-enabled environments such as NativeAOT.
	///   Otherwise, methods such as <see cref="ISerializer.ToString" /> may throw <see cref="InvalidOperationException" />.
	/// </remarks>
	[RequiresDynamicCode("Default behavior requires Reflection. For trimming support, use: AddJsonSerialization(IServiceCollection, HostBuilderContext, IEnumerable<IJsonTypeInfoResolver>).")]
	[RequiresUnreferencedCode("Default behavior requires Reflection. For trimming support, use: AddJsonSerialization(IServiceCollection, HostBuilderContext, IEnumerable<IJsonTypeInfoResolver>).")]
	public static IServiceCollection AddSystemTextJsonSerialization(
		this IServiceCollection services,
		HostBuilderContext context)
	{
		if (context.IsRegistered(nameof(AddSystemTextJsonSerialization)))
		{
			return services;
		}

		return services
			.AddSingleton(sp => new JsonSerializerOptions(JsonSerializationOptions.DefaultSerializerOptions))
			.AddSingleton<SystemTextJsonSerializer>()
			.AddSingleton<ISerializer>(services => services.GetRequiredService<SystemTextJsonSerializer>())
			.AddSingleton(typeof(ISerializer<>), typeof(SystemTextJsonGeneratedSerializer<>));
	}

	/// <summary>
	/// Adds serialization-related metadata for type <typeparamref name="TEntity"/> to the <see cref="IServiceCollection"/>.
	/// </summary>
	/// <typeparam name="TEntity">
	/// The type to add serialization-related metadata for.
	/// </typeparam>
	/// <param name="services">
	/// The <see cref="IServiceCollection"/> to add serialization-related metadata to.
	/// </param>
	/// <param name="instance">
	/// An object which contains serialization-related metadata for type <typeparamref name="TEntity"/>.
	/// </param>
	/// <returns>
	/// The <see cref="IServiceCollection"/> with serialization-related metadata for type <typeparamref name="TEntity"/> added.
	/// </returns>
	public static IServiceCollection AddJsonTypeInfo<TEntity>(
		this IServiceCollection services,
		JsonTypeInfo<TEntity> instance
		)
	{
		if (instance.OriginatingResolver is {} resolver)
		{
			AddJsonTypeInfo(services, resolver);
		}
		return services
			.AddSingleton(instance)
			.AddSingleton<ISerializerTypedInstance>(sp => new SerializerTypedInstance<TEntity>(sp, instance));
	}

	/// <summary>
	///   Configures options used for reading and writing JSON when using
	///   <see cref="SystemTextJsonSerializer" />-backed <see cref="ISerializer" /> instances.
	/// </summary>
	/// <param name="services">
	///   The <see cref="IServiceCollection"/> to configure options on.
	/// </param>
	/// <param name="configureOptions">
	///   The <see cref="Action{JsonSerializationOptions}" /> to configure the <see cref="JsonSerializationOptions" />
	/// </param>
	/// <returns>
	///   The modified <see cref="IServiceCollection" />.
	/// </returns>
	public static IServiceCollection ConfigureJsonSerializationOptions(this IServiceCollection services, Action<JsonSerializationOptions> configureOptions)
	{
		services.Configure<JsonSerializationOptions>(configureOptions);
		return services;
	}

	/// <summary>
	///   Adds additional <see cref="IJsonTypeInfoResolver"/> instances to use for JSON serialization and deserialization.
	/// </summary>
	/// <param name="services">
	///   The <see cref="IServiceCollection"/> to add <see cref="IJsonTypeInfoResolver"/> instances to.
	/// </param>
	/// <param name="typeInfoResolvers">
	///   An enumerable of <see cref="IJsonTypeInfoResolver" /> instances to use for JSON serialization and deserialization.
	/// </param>
	/// <returns>
	///   The modified <see cref="IServiceCollection" />.
	/// </returns>
	public static IServiceCollection AddJsonTypeInfo(this IServiceCollection services, params IEnumerable<IJsonTypeInfoResolver> typeInfoResolvers)
	{
		return services.ConfigureJsonSerializationOptions(options =>
		{
			foreach (var resolver in typeInfoResolvers)
			{
				options.SerializerOptions.TypeInfoResolverChain.Add(resolver);
			}
		});
	}
}

internal interface ISerializerTypedInstance : ISerializer
{
	Type JsonType { get; }
}

internal record SerializerTypedInstance<T>(IServiceProvider Services, JsonTypeInfo<T> TypeInfo) : ISerializerTypedInstance
{
	public Type JsonType => typeof(T);

	private ISerializer<T> Serializer => Services.GetRequiredService<ISerializer<T>>();
	public object? FromString(string source, Type targetType) => Serializer.FromString(source);
	public object? FromStream(Stream source, Type targetType) => Serializer.FromStream(source);
	public string ToString(object value, Type valueType) => Serializer.ToString((T)value);
	public void ToStream(Stream stream, object value, Type valueType) => Serializer.ToStream(stream, (T)value);
}
