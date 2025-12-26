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
	public static IServiceCollection AddSerialization(
		this IServiceCollection services,
		HostBuilderContext context)
	{
		if (context.IsRegistered(nameof(AddSerialization)))
		{
			return services;
		}
		return services
			.AddSingleton<SystemTextJsonSerializer>()
			.AddSingleton<ISerializer>(services => services.GetRequiredService<SystemTextJsonSerializer>())
			.AddSingleton(typeof(ISerializer<>), typeof(SystemTextJsonGeneratedSerializer<>));
	}

	/// <summary>
	/// Adds the serialization services to the <see cref="IServiceCollection"/>.
	/// </summary>
	/// <param name="services">Service collection.</param>
	/// <param name="context">The <see cref="HostBuilderContext"/> to use when adding services</param>
	/// <returns><see cref="IServiceCollection"/>.</returns>
	[RequiresDynamicCode("Default behavior requires Reflection. Use AddSerialization() + AddJsonTypeInfoResolvers() instead.")]
	[RequiresUnreferencedCode("Default behavior requires Reflection. Use AddSerialization() + AddJsonTypeInfoResolvers() instead.")]
	public static IServiceCollection AddSystemTextJsonSerialization(
		this IServiceCollection services,
		HostBuilderContext context)
	{
		if (context.IsRegistered(nameof(AddSystemTextJsonSerialization)))
		{
			return services;
		}

		return services
			.AddSingleton(sp => new JsonSerializerOptions(SerializationOptions.DefaultSerializerOptions))
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
			AddSerializationJsonTypeInfoResolvers(services, resolver);
		}
		return services
			.AddSingleton(instance)
			.AddSingleton<ISerializerTypedInstance>(sp => new SerializerTypedInstance<TEntity>(sp, instance));
	}

	public static IServiceCollection ConfigureSerializationOptions(this IServiceCollection services, Action<SerializationOptions> configureOptions)
	{
		services.Configure<SerializationOptions>(configureOptions);
		return services;
	}

	public static IServiceCollection AddSerializationJsonTypeInfoResolvers(this IServiceCollection services, params IJsonTypeInfoResolver[] typeInfoResolvers)
	{
		return services.ConfigureSerializationOptions(options =>
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
