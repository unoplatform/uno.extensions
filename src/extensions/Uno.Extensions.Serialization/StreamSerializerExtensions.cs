using System;
using System.IO;

namespace Uno.Extensions.Serialization;

public static class StreamSerializerExtensions
{
    public static T? ReadFromStream<T>(this IStreamSerializer serializer, Stream stream)
    {
        return serializer is not null ?
            (serializer.ReadFromStream(stream, typeof(T)) is T tvalue) ?
                tvalue :
                default :
            default;
    }

    public static T? FromStream<T>(this IStreamSerializer serializer, Stream stream)
    {
        if (stream == null)
        {
            return default;
        }

        var pos = stream.Position;
        var value = serializer is not null ?
            (serializer.ReadFromStream(stream, typeof(T)) is T tvalue) ?
                tvalue :
                default :
            default;
        stream.Seek(pos, SeekOrigin.Begin);
        return value;
    }

    public static IStreamSerializer WriteToStream<T>(this IStreamSerializer serializer, Stream stream, T value)
    {
        if (value is not null)
        {
            serializer.WriteToStream(stream, value, typeof(T));
        }
        return serializer;
    }

    public static IStreamSerializer WriteToStream(this IStreamSerializer serializer, Stream stream, object value)
    {
        serializer.WriteToStream(stream, value, value.GetType());
        return serializer;
    }

    public static Stream? ToStream<T>(this IStreamSerializer serializer, T value)
    {
        if (value is not null)
        {
            return serializer.ToStream(value, typeof(T));
        }

        return default;
    }

    public static Stream ToStream(this IStreamSerializer serializer, object value, Type valueType)
    {
        var memoryStream = new MemoryStream();

        serializer?.WriteToStream(memoryStream, value, valueType);
        memoryStream.Seek(0, SeekOrigin.Begin);

        return memoryStream;
    }
}
