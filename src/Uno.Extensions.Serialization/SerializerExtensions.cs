namespace Uno.Extensions.Serialization;

/// <summary>
/// Extensions for <see cref="ISerializer"/> to serialize and deserialize objects.
/// </summary>
public static class SerializerExtensions
{
	/// <summary>
	/// Creates a serialized representation of an object.
	/// </summary>
	/// <typeparam name="T">
	/// The type of the object to serialize.
	/// </typeparam>
	/// <param name="serializer">
	/// The <see cref="ISerializer"/> to use to serialize the object.
	/// </param>
	/// <param name="value">
	/// The object to serialize.
	/// </param>
	/// <returns>
	/// The serialized representation of value.
	/// </returns>
	public static string ToString<T>(this ISerializer serializer, T value) =>
		value is not null ?
			serializer.ToString(value, typeof(T)) :
			string.Empty;

	/// <summary>
	/// Creates an instance of T from a serialized representation.
	/// </summary>
	/// <typeparam name="T">
	/// The type to use to deserialize the source.
	/// </typeparam>
	/// <param name="serializer">
	/// The <see cref="ISerializer"/> to use to deserialize the source.
	/// </param>
	/// <param name="valueAsString">
	/// A serialized representation of a T.
	/// </param>
	/// <returns>
	/// The instance of T deserialized from the source.
	/// </returns>
	public static T? FromString<T>(this ISerializer serializer, string valueAsString)
	{
		return serializer is not null ?
		(serializer.FromString(valueAsString, typeof(T)) is T tvalue) ?
				tvalue :
				default :
			default;
	}

	/// <summary>
	/// Creates an instance of T from a serialized representation.
	/// </summary>
	/// <typeparam name="T">
	/// The type to use to deserialize the source.
	/// </typeparam>
	/// <param name="serializer">
	/// The <see cref="ISerializer"/> to use to deserialize the source.
	/// </param>
	/// <param name="stream">
	/// A serialized representation of a T.
	/// </param>
	/// <returns>
	/// The instance of T deserialized from the source.
	/// </returns>
	public static T? FromStream<T>(this ISerializer serializer, Stream stream)
	{
		return (serializer.FromStream(stream, typeof(T)) is T tvalue) ?
					tvalue :
					default;
	}

	/// <summary>
	/// Write a serialized representation of an object to a given System.IO.Stream.
	/// </summary>
	/// <typeparam name="T">
	/// The type of the object to serialize.
	/// </typeparam>
	/// <param name="serializer">
	/// The <see cref="ISerializer"/> to use to serialize the object.
	/// </param>
	/// <param name="stream">
	/// The stream to which the value should be written.
	/// </param>
	/// <param name="value">
	/// The object to serialize.
	/// </param>
	/// <returns>
	/// The <see cref="ISerializer"/> to use to serialize the object.
	/// </returns>
	public static ISerializer ToStream<T>(this ISerializer serializer, Stream stream, T value)
	{
		if (value is not null)
		{
			serializer.ToStream(stream, value, typeof(T));
		}
		return serializer;
	}

	/// <summary>
	/// Write a serialized representation of an object to a given System.IO.Stream.
	/// </summary>
	/// <typeparam name="T">
	/// The type of the object to serialize.
	/// </typeparam>
	/// <param name="serializer">
	/// The <see cref="ISerializer"/> to use to serialize the object.
	/// </param>
	/// <param name="value">
	/// The object to serialize.
	/// </param>
	/// <returns>
	/// A stream containing the serialized representation of value.
	/// </returns>
	public static Stream? ToStream<T>(this ISerializer serializer, T value)
	{
		if (value is not null)
		{
			return serializer.ToStream(value, typeof(T));
		}

		return default;
	}

	/// <summary>
	/// Creates a serialized representation of an object.
	/// </summary>
	/// <param name="serializer">
	/// The <see cref="ISerializer"/> to use to serialize the object.
	/// </param>
	/// <param name="value">
	/// The object to serialize.
	/// </param>
	/// <param name="valueType">
	/// The type to use to serialize the object. value must be convertible to this type.
	/// </param>
	/// <returns>
	/// The serialized representation of value.
	/// </returns>
	public static Stream ToStream(this ISerializer serializer, object value, Type valueType)
	{
		var memoryStream = new MemoryStream();

		serializer?.ToStream(memoryStream, value, valueType);
		memoryStream.Seek(0, SeekOrigin.Begin);

		return memoryStream;
	}
}
