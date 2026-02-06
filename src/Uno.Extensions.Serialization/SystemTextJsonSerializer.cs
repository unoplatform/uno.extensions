using System.Diagnostics.CodeAnalysis;

using Microsoft.Extensions.Options;

namespace Uno.Extensions.Serialization;

/// <summary>
/// A reflection-based serializer implementation for System.Text.Json.
/// </summary>
public class SystemTextJsonSerializer : ISerializer
{
	const string SuppressRequiresDynamicCodeMessage = "Reflection is optional, based on JsonSerializer.IsReflectionEnabledByDefault. Exception message describes how use System.Text.Json source generation output.";
	const string SuppressRequiresUnreferencedCodeMessage = "Reflection is optional, based on JsonSerializer.IsReflectionEnabledByDefault. Exception message describes how use System.Text.Json source generation output.";

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

		// Need to use `.GetService<…>()` in order for `.ConfigureJsonSerializationOptions()` callbacks to be invoked.
		_serializerOptions = services.GetJsonSerializationOptions() ??
			serializerOptions ??
			JsonSerializationOptions.DefaultSerializerOptions;
	}

	private bool TryGetJsonTypeInfo(Type type, [NotNullWhen(true)] out JsonTypeInfo? info)
		=> _serializerOptions.TryGetTypeInfo(type, out info);

	private static Exception CreateInvalidOperationException(Type targetType)
		=> new InvalidOperationException("Reflection-based serialization has been disabled for this application. " +
			$"Use the IServiceCollection.{nameof(ServiceCollectionExtensions.AddJsonTypeInfo)}() or IHostBuilder.{HostBuilderExtensions.TrimSafeUseSerializationOverload} extension methods to enable JSON deserialization " +
			$"for type `{targetType.FullName}`.");

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
	/// <exception cref="InvalidOperationException">
	///   No support was found for deserializing <paramref name="targetType" />.
	///   Use <see cref="ServiceCollectionExtensions.AddJsonTypeInfo" /> to add support.
	/// </exception>
	[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = SuppressRequiresUnreferencedCodeMessage)]
	[UnconditionalSuppressMessage("Trimming", "IL3050", Justification = SuppressRequiresDynamicCodeMessage)]
	public object? FromStream(Stream source, Type targetType)
	{
		var typedSerializer = TypedSerializer(targetType);
		if (typedSerializer is not null)
		{
			return typedSerializer.FromStream(source, targetType);
		}
		else if (TryGetJsonTypeInfo(targetType, out var info))
		{
			return JsonSerializer.Deserialize(source, info);
		}
		else if (JsonSerializer.IsReflectionEnabledByDefault)
		{
			return JsonSerializer.Deserialize(source, targetType, _serializerOptions);
		}
		else
		{
			throw CreateInvalidOperationException(targetType);
		}
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
	/// <exception cref="InvalidOperationException">
	///   No support was found for serializing <paramref name="valueType" />.
	///   Use <see cref="ServiceCollectionExtensions.AddJsonTypeInfo" /> to add support.
	/// </exception>
	[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = SuppressRequiresUnreferencedCodeMessage)]
	[UnconditionalSuppressMessage("Trimming", "IL3050", Justification = SuppressRequiresDynamicCodeMessage)]
	public void ToStream(Stream stream, object value, Type valueType)
	{
		var typedSerializer = TypedSerializer(valueType);
		if (typedSerializer is not null)
		{
			typedSerializer.ToStream(stream, value);
		}
		else if (TryGetJsonTypeInfo(valueType, out var info))
		{
			JsonSerializer.Serialize(stream, value, info);
		}
		else if (JsonSerializer.IsReflectionEnabledByDefault)
		{
			JsonSerializer.Serialize(stream, value, valueType, _serializerOptions);
		}
		else
		{
			throw CreateInvalidOperationException(valueType);
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
	/// <exception cref="InvalidOperationException">
	///   No support was found for serializing <paramref name="valueType" />.
	///   Use <see cref="ServiceCollectionExtensions.AddJsonTypeInfo" /> to add support.
	/// </exception>
	[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = SuppressRequiresUnreferencedCodeMessage)]
	[UnconditionalSuppressMessage("Trimming", "IL3050", Justification = SuppressRequiresDynamicCodeMessage)]
	public string ToString(object value, Type valueType)
	{
		var typedSerializer = TypedSerializer(valueType);
		if (typedSerializer is not null)
		{
			return typedSerializer.ToString(value, valueType);
		}
		else if (TryGetJsonTypeInfo(valueType, out var info))
		{
			return JsonSerializer.Serialize(value, info);
		}
		else if (JsonSerializer.IsReflectionEnabledByDefault)
		{
			return JsonSerializer.Serialize(value, valueType, _serializerOptions);
		}
		else
		{
			throw CreateInvalidOperationException(valueType);
		}
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
	/// <exception cref="InvalidOperationException">
	///   No support was found for deserializing <paramref name="targetType" />.
	///   Use <see cref="ServiceCollectionExtensions.AddJsonTypeInfo" /> to add support.
	/// </exception>
	[UnconditionalSuppressMessage("Trimming", "IL2026", Justification = SuppressRequiresUnreferencedCodeMessage)]
	[UnconditionalSuppressMessage("Trimming", "IL3050", Justification = SuppressRequiresDynamicCodeMessage)]
	public object? FromString(string source, Type targetType)
	{
		var typedSerializer = TypedSerializer(targetType);
		if (typedSerializer is not null)
		{
			return typedSerializer.FromString(source, targetType);
		}
		else if (TryGetJsonTypeInfo(targetType, out var info))
		{
			return JsonSerializer.Deserialize(source, info);
		}
		else if (JsonSerializer.IsReflectionEnabledByDefault)
		{
			return JsonSerializer.Deserialize(source, targetType, _serializerOptions);
		}
		else
		{
			throw CreateInvalidOperationException(targetType);
		}
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
