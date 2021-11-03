using System;
using System.IO;

namespace Uno.Extensions.Serialization
{
    public static class StreamSerializerExtensions
    {
        public static T? ReadFromStream<T>(this ISerializer serializer, Stream stream)
        {
            return serializer is not null ?
                (serializer.ReadFromStream(stream, typeof(T)) is T tvalue) ?
                    tvalue :
                    default :
                default;
        }

        public static T? FromStream<T>(this ISerializer serializer, Stream stream)
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

        public static ISerializer WriteToStream<T>(this ISerializer serializer, Stream stream, T value)
        {
            if (value is not null)
            {
                serializer.WriteToStream(stream, value, typeof(T));
            }
            return serializer;
        }

        public static ISerializer WriteToStream(this ISerializer serializer, Stream stream, object value)
        {
            serializer.WriteToStream(stream, value, value.GetType());
            return serializer;
        }

        public static Stream? ToStream<T>(this ISerializer serializer, T value)
        {
            if (value is not null)
            {
                return serializer.ToStream(value, typeof(T));
            }

            return default;
        }

        public static Stream ToStream(this ISerializer serializer, object value, Type valueType)
        {
            var memoryStream = new MemoryStream();

            serializer?.WriteToStream(memoryStream, value, valueType);
            memoryStream.Seek(0, SeekOrigin.Begin);

            return memoryStream;
        }

        public static string ToString<T>(this ISerializer serializer, T value)
        {
            if (value is not null)
            {
                return serializer.ToString(value, typeof(T));
            }

            return string.Empty;
        }

        public static T? FromString<T>(this ISerializer serializer, string valueAsString)
        {
            return serializer is not null ?
            (serializer.FromString(valueAsString, typeof(T)) is T tvalue) ?
                    tvalue :
                    default :
                default;
        }
    }
}
