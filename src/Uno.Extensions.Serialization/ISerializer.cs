namespace Uno.Extensions.Serialization;

public interface ISerializer
{
    // Summary:
    //     Creates a serialized representation of an object.
    //
    // Parameters:
    //   value:
    //     The object to serialize.
    //
    //   valueType:
    //     The type to use to serialize the object. value must be convertible to this type.
    //
    // Returns:
    //     The serialized representation of value.
    string ToString(object value, Type valueType);

    // Summary:
    //     Creates an instance of targetType from a serialized representation.
    //
    // Parameters:
    //   source:
    //     A serialized representation of a targetType.
    //
    //   targetType:
    //     The type to use to deserialize the source.
    //
    // Returns:
    //     The instance of targetType deserialized from the source.
    object? FromString(string source, Type targetType);

	// Summary:
	//     Creates an instance of targetType from a serialized representation.
	//
	// Parameters:
	//   source:
	//     A serialized representation of a targetType.
	//
	//   targetType:
	//     The type to use to deserialize the source.
	//
	// Returns:
	//     The instance of targetType deserialized from the source.
	object? FromStream(Stream source, Type targetType);

	// Summary:
	//     Write a serialized representation of an object to a given System.IO.Stream.
	//
	// Parameters:
	//   value:
	//     The object to serialize.
	//
	//   valueType:
	//     The type to use to serialize the object. value must be convertible to this type.
	//
	//   stream:
	//     The stream to which the value should be written.
	//
	//   canDisposeStream:
	//     A bool which indicates if the stream can be disposed after having written the
	//     object or not.
	void ToStream(Stream stream, object value, Type valueType);
}

public interface ISerializer<T> : ISerializer
{
	string ToString(T value);
	T? FromString(string source);

	T? FromStream(Stream source);
	void ToStream(Stream stream, T value);
}
