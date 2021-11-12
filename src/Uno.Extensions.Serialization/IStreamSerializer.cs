using System;
using System.IO;

namespace Uno.Extensions.Serialization;

public interface IStreamSerializer
{
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
    object? ReadFromStream(Stream source, Type targetType);

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
    void WriteToStream(Stream stream, object value, Type valueType);
}
