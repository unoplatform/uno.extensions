using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization.Metadata;

namespace Uno.Extensions.Serialization;

/// <summary>
/// A reflection-based serializer implementation for System.Text.Json.
/// </summary>
public class SystemTextJsonSerializer : ISerializer
{
	private readonly JsonSerializerOptions _serializerOptions;
	private readonly IServiceProvider _services;

	private ISerializerTypedInstance? TypedSerializer(Type jsonType) => _services.GetServices<ISerializerTypedInstance>().FirstOrDefault(x => x.JsonType == jsonType);

	/// <summary>
	/// Creates a new <see cref="SystemTextJsonSerializer"/> instance.
	/// </summary>
	/// <param name="services">
	/// The <see cref="IServiceProvider"/> to use to resolve <see cref="ISerializerTypedInstance"/> instances.
	/// </param>
	/// <param name="serializerOptions">
	/// An instance of <see cref="JsonSerializerOptions"/> to use when serializing and deserializing objects. Optional
	/// </param>
	public SystemTextJsonSerializer(IServiceProvider services, JsonSerializerOptions? serializerOptions = null)
	{
		_services = services;
		_serializerOptions = ConfigureSerializerOptions(serializerOptions);
	}

	private static JsonSerializerOptions ConfigureSerializerOptions(JsonSerializerOptions? options)
	{
		// Create a new options instance or clone the provided one to avoid modifying shared instances
		var configuredOptions = options is null
			? new JsonSerializerOptions()
			: new JsonSerializerOptions(options);

		// Configure TypeInfoResolver to support both reflection-based and AOT scenarios.
		// Use DefaultJsonTypeInfoResolver for reflection-based serialization combined with
		// CommonTypesJsonSerializerContext for common types that have source-generated metadata.
		if (configuredOptions.TypeInfoResolver is null)
		{
			configuredOptions.TypeInfoResolver = JsonTypeInfoResolver.Combine(
				new DefaultJsonTypeInfoResolver(),
				CommonTypesJsonSerializerContext.Default);
		}
		else
		{
			// User provided a resolver - combine it with default resolver and common types
			configuredOptions.TypeInfoResolver = JsonTypeInfoResolver.Combine(
				configuredOptions.TypeInfoResolver,
				new DefaultJsonTypeInfoResolver(),
				CommonTypesJsonSerializerContext.Default);
		}

		return configuredOptions;
	}

	/// <summary>
	/// Creates an object of type <paramref name="targetType"/> from a serialized (stream) representation.
	/// </summary>
	/// <param name="source">
	/// A stream containing the serialized representation of targetType.
	/// </param>
	/// <param name="targetType">
	/// The type to use to deserialize the source.
	/// </param>
	/// <returns>
	/// The instance of targetType deserialized from the source.
	/// </returns>
	[RequiresUnreferencedCode("From JsonDeserializer: JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
	public object? FromStream(Stream source, Type targetType)
	{
		var typedSerializer = TypedSerializer(targetType);
		return typedSerializer is not null ? typedSerializer.FromStream(source, targetType) : JsonSerializer.Deserialize(source, targetType, _serializerOptions);
	}

	/// <summary>
	/// Creates a serialized (stream) representation of an object.
	/// </summary>
	/// <param name="stream">
	/// The stream to which the value should be written.
	/// </param>
	/// <param name="value">
	/// The object to serialize.
	/// </param>
	/// <param name="valueType">
	/// The type to use to serialize the object. value must be convertible to this type.
	/// </param>
	[RequiresUnreferencedCode("From JsonDeserializer: JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
	public void ToStream(Stream stream, object value, Type valueType)
	{
		var typedSerializer = TypedSerializer(valueType);
		if (typedSerializer is not null)
		{
			typedSerializer.ToStream(stream, value);
		}
		else
		{
			JsonSerializer.Serialize(stream, value, valueType, _serializerOptions);
		}
	}

	/// <summary>
	/// Creates a serialized representation of an object.
	/// </summary>
	/// <param name="value">
	/// The object to serialize.
	/// </param>
	/// <param name="valueType">
	/// The type to use to serialize the object. value must be convertible to this type.
	/// </param>
	/// <returns>
	/// The serialized representation of value.
	/// </returns>
	[RequiresUnreferencedCode("From JsonDeserializer: JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
	public string ToString(object value, Type valueType)
	{
		var typedSerializer = TypedSerializer(valueType);
		return typedSerializer is not null ? typedSerializer.ToString(value, valueType) : JsonSerializer.Serialize(value, valueType, _serializerOptions);
	}

	/// <summary>
	/// Creates an instance of targetType from a serialized representation.
	/// </summary>
	/// <param name="source">
	/// A serialized representation of a targetType instance.
	/// </param>
	/// <param name="targetType">
	/// The type to use to deserialize the source.
	/// </param>
	/// <returns>
	/// The instance of targetType deserialized from the source.
	/// </returns>
	[RequiresUnreferencedCode("From JsonDeserializer: JSON serialization and deserialization might require types that cannot be statically analyzed and might need runtime code generation. Use System.Text.Json source generation for native AOT applications.")]
	public object? FromString(string source, Type targetType)
	{
		var typedSerializer = TypedSerializer(targetType);
		return typedSerializer is not null ? typedSerializer.FromString(source, targetType) : JsonSerializer.Deserialize(source, targetType, _serializerOptions);
	}
}

/// <summary>
/// A reflection-based serializer implementation for System.Text.Json.
/// </summary>
/// <typeparam name="T">
/// The type of the objects to serialize and deserialize.
/// </typeparam>
public class SystemTextJsonSerializer<T> : SystemTextJsonSerializer, ISerializer<T>
{
	/// <summary>
	/// Creates a new <see cref="SystemTextJsonSerializer{T}"/> instance.
	/// </summary>
	/// <param name="services">
	/// The <see cref="IServiceProvider"/> to use to resolve <see cref="ISerializerTypedInstance"/> instances.
	/// </param>
	/// <param name="serializerOptions">
	/// An instance of <see cref="JsonSerializerOptions"/> to use when serializing and deserializing objects. Optional
	/// </param>
	public SystemTextJsonSerializer(
		IServiceProvider services,
		JsonSerializerOptions? serializerOptions = null) : base(services, serializerOptions)
	{
	}

	/// <inheritdoc/>
	public T? FromString(string source)
	{
		return FromString(source, typeof(T)) is T value ? value : default;
	}

	/// <inheritdoc/>
	public T? FromStream(Stream source)
	{
		return FromStream(source, typeof(T)) is T value ? value : default;
	}

	/// <inheritdoc/>
	public string ToString(T value)
	{
		if (value is null)
		{
			return string.Empty;
		}

		return ToString(value, typeof(T));
	}

	/// <inheritdoc/>
	public void ToStream(Stream stream, T value)
	{
		if (value is null)
		{
			return;
		}

		ToStream(stream, value, typeof(T));
	}
}
