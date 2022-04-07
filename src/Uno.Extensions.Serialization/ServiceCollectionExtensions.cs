namespace Uno.Extensions.Serialization;

/// <summary>
/// This class is used for serialization configuration.
/// - Configures the serializers.
/// </summary>
public static class ServiceCollectionExtensions
{
	/// <summary>
	/// Adds the serialization services to the <see cref="IServiceCollection"/>.
	/// </summary>
	/// <param name="services">Service collection.</param>
	/// <returns><see cref="IServiceCollection"/>.</returns>
	public static IServiceCollection AddSystemTextJsonSerialization(
		this IServiceCollection services)
	{
		return services
			.AddSingleton(sp => new JsonSerializerOptions
			{
				NumberHandling = System.Text.Json.Serialization.JsonNumberHandling.AllowReadingFromString,
				AllowTrailingCommas = true
			})
			.AddSingleton<SystemTextJsonStreamSerializer>()
			.AddSingleton<ISerializer>(services => services.GetRequiredService<SystemTextJsonStreamSerializer>())
			.AddSingleton(typeof(ISerializer<>), typeof(SystemTextJsonGeneratedSerializer<>))
			.AddSingleton<IStreamSerializer>(services => services.GetRequiredService<SystemTextJsonStreamSerializer>())
			.AddSingleton(typeof(IStreamSerializer<>), typeof(SystemTextJsonGeneratedSerializer<>));
	}

	public static IServiceCollection AddJsonTypeInfo<TEntity>(
		this IServiceCollection services,
		JsonTypeInfo<TEntity> instance
		)
	{
		return services
			.AddSingleton(instance)
			.AddSingleton<IJsonTypeInfoWrapper>(sp => new JsonTypeInfoWrapper<TEntity>(sp, instance));
	}
}

internal interface IJsonTypeInfoWrapper : ISerializer, IStreamSerializer
{
	Type JsonType { get; }
}

internal record JsonTypeInfoWrapper<T>(IServiceProvider Services, JsonTypeInfo<T> TypeInfo) : IJsonTypeInfoWrapper
{
	public Type JsonType => typeof(T);

	private ISerializer<T> Serializer => Services.GetRequiredService<ISerializer<T>>();
	private IStreamSerializer<T> StreamSerializer => Services.GetRequiredService<IStreamSerializer<T>>();
	public object? FromString(string source, Type targetType) => Serializer.FromString(source);
	public object? ReadFromStream(Stream source, Type targetType) => StreamSerializer.ReadFromStream(source);
	public string ToString(object value, Type valueType) => Serializer.ToString((T)value);
	public void WriteToStream(Stream stream, object value, Type valueType) => StreamSerializer.WriteToStream(stream, (T)value);
}
