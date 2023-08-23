namespace Uno.Extensions.Serialization;

/// <summary>
/// An interface for serializing and deserializing objects.
/// </summary>
public interface ISerializer
{
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
	string ToString(object value, Type valueType);

	/// <summary>
	/// Creates an instance of targetType from a serialized representation.
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
	object? FromString(string source, Type targetType);

	/// <summary>
	/// Creates an instance of targetType from a serialized representation.
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
	object? FromStream(Stream source, Type targetType);

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
	void ToStream(Stream stream, object value, Type valueType);
}

/// <summary>
/// An interface for serializing and deserializing objects of type T.
/// </summary>
/// <typeparam name="T">
/// The type of object to serialize and deserialize back to.
/// </typeparam>
public interface ISerializer<T> : ISerializer
{
	/// <summary>
	/// Creates a serialized representation of an object of type T.
	/// </summary>
	/// <param name="value">
	/// The object to serialize.
	/// </param>
	/// <returns>
	/// The serialized representation of value.
	/// </returns>
	string ToString(T value);

	/// <summary>
	/// Creates an instance of T from a serialized representation.
	/// </summary>
	/// <param name="source">
	/// A serialized representation of a T.
	/// </param>
	/// <returns>
	/// The instance of T deserialized from the source.
	/// </returns>
	T? FromString(string source);

	/// <summary>
	/// Creates an instance of T from a serialized representation.
	/// </summary>
	/// <param name="source">
	/// A serialized representation of a T.
	/// </param>
	/// <returns>
	/// The instance of T deserialized from the source.
	/// </returns>
	T? FromStream(Stream source);

	/// <summary>
	/// Write a serialized representation of an object of type T to a given System.IO.Stream.
	/// </summary>
	/// <param name="stream">
	/// The stream to which the value should be written.
	/// </param>
	/// <param name="value">
	/// The object to serialize.
	/// </param>
	void ToStream(Stream stream, T value);
}
