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
				DefaultIgnoreCondition= System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault,
				AllowTrailingCommas = true
			})
			.AddSingleton<SystemTextJsonSerializer>()
			.AddSingleton<ISerializer>(services => services.GetRequiredService<SystemTextJsonSerializer>())
			.AddSingleton(typeof(ISerializer<>), typeof(SystemTextJsonGeneratedSerializer<>));
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

internal interface IJsonTypeInfoWrapper : ISerializer
{
	Type JsonType { get; }
}

internal record JsonTypeInfoWrapper<T>(IServiceProvider Services, JsonTypeInfo<T> TypeInfo) : IJsonTypeInfoWrapper
{
	public Type JsonType => typeof(T);

	private ISerializer<T> Serializer => Services.GetRequiredService<ISerializer<T>>();
	public object? FromString(string source, Type targetType) => Serializer.FromString(source);
	public object? FromStream(Stream source, Type targetType) => Serializer.FromStream(source);
	public string ToString(object value, Type valueType) => Serializer.ToString((T)value);
	public void ToStream(Stream stream, object value, Type valueType) => Serializer.ToStream(stream, (T)value);
}
