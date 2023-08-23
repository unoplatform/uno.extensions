namespace Uno.Extensions.Serialization;

/// <summary>
/// A serializer implementation that leverages the source generation feature of System.Text.Json
/// introduced in .NET 6.0.
/// </summary>
/// <typeparam name="T">
/// The type to serialize and deserialize.
/// </typeparam>
public class SystemTextJsonGeneratedSerializer<T> : ISerializer<T>
{
	/// <summary>
	/// Creates a new <see cref="SystemTextJsonGeneratedSerializer{T}"/> instance.
	/// </summary>
	/// <param name="nonTypedSerializer">
	/// An instance of a reflection-based serializer to use when the type is not known at compile time.
	/// </param>
	/// <param name="typeInfo">
	/// An instance of <see cref="JsonTypeInfo{T}"/> that contains the serialization-related metadata for type <typeparamref name="T"/>. Optional
	/// </param>
	public SystemTextJsonGeneratedSerializer(
		ISerializer nonTypedSerializer,
		JsonTypeInfo<T>? typeInfo = null)
	{
		_nonTypedSerializer = nonTypedSerializer;
		_typeInfo = typeInfo;
	}

	/// <summary>
	/// Creates a serialized representation of an object.
	/// </summary>
	/// <param name="value">
	/// The object to serialize.
	/// </param>
	/// <returns>
	/// The serialized representation of value.
	/// </returns>
	public string ToString(T value)
	{
		if (_typeInfo is not null)
		{
			return JsonSerializer.Serialize(value, _typeInfo);
		}

		return _nonTypedSerializer.ToString(value);
	}

	/// <summary>
	/// Creates an instance of T from a serialized representation.
	/// </summary>
	/// <param name="source">
	/// A serialized representation needed to create the object of T.
	/// </param>
	/// <returns>
	/// The instance of T deserialized from the source.
	/// </returns>
	public T? FromString(string source)
	{
		if (_typeInfo is not null)
		{
			return JsonSerializer.Deserialize(source, _typeInfo);
		}

		return _nonTypedSerializer.FromString<T>(source);
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
	public string ToString(object value, Type valueType) => valueType == typeof(T) ? ToString((T)value) : _nonTypedSerializer.ToString(value, valueType);

	/// <summary>
	/// Creates an instance of the target type from a serialized representation.
	/// </summary>
	/// <param name="source">
	/// A serialized representation of a targetType.
	/// </param>
	/// <param name="targetType">
	/// The type to use to deserialize the source.
	/// </param>
	/// <returns>
	/// The instance of targetType deserialized from the source.
	/// </returns>
	public object? FromString(string source, Type targetType) => targetType == typeof(T) ? FromString(source) : _nonTypedSerializer.FromString(source, targetType);

	/// <summary>
	/// Creates an instance of T from a serialized representation.
	/// </summary>
	/// <param name="source">
	/// A serialized (stream) representation of a T.
	/// </param>
	/// <returns>
	/// The instance of T deserialized from the source.
	/// </returns>
	public T? FromStream(Stream source)
	{
		if (_typeInfo is not null)
		{
			return JsonSerializer.Deserialize(source, _typeInfo);
		}

		return _nonTypedSerializer.FromStream<T>(source);
	}

	/// <summary>
	/// Write a serialized representation of an object to a given System.IO.Stream.
	/// </summary>
	/// <param name="stream">
	/// The stream to which the value should be written.
	/// </param>
	/// <param name="value">
	/// The object to serialize.
	/// </param>
	public void ToStream(Stream stream, T value)
	{
		if (_typeInfo is not null)
		{
			JsonSerializer.Serialize(stream, value, _typeInfo);
			return;
		}

		_nonTypedSerializer.ToStream<T>(stream, value);
	}

	/// <summary>
	/// Creates an instance of the target type from a serialized (stream) representation.
	/// </summary>
	/// <param name="source">
	/// A serialized (stream) representation of a targetType.
	/// </param>
	/// <param name="targetType">
	/// The type to use to deserialize the source.
	/// </param>
	/// <returns>
	/// The instance of targetType deserialized from the source.
	/// </returns>
	public object? FromStream(Stream source, Type targetType) => targetType == typeof(T) ? FromStream(source) : _nonTypedSerializer.FromStream(source, targetType);

	/// <summary>
	/// Write a serialized representation of an object to a given System.IO.Stream.
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
	public void ToStream(Stream stream, object value, Type valueType)
	{
		if (valueType == typeof(T))
		{

			ToStream(stream, (T)value);
		}
		else
		{
			_nonTypedSerializer.ToStream(stream, value, valueType);
		}
	}

	private readonly ISerializer _nonTypedSerializer;
	private readonly JsonTypeInfo<T>? _typeInfo;
}
