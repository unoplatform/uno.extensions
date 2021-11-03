﻿using System;

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
}
