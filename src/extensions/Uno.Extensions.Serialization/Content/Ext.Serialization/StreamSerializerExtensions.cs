using System;
using System.IO;

namespace Uno.Extensions.Serialization
{
    public static class StreamSerializerExtensions
    {
        public static T ReadFromStream<T>(this ISerializer serializer, Stream stream)
        {
            return serializer is not null ?
                (T)serializer.ReadFromStream(stream, typeof(T)) :
                default;
        }

        public static T FromStream<T>(this ISerializer serializer, Stream stream)
        {
            if (stream == null)
            {
                return default;
            }

            var pos = stream.Position;
            var value = serializer is not null ?
                (T)serializer.ReadFromStream<T>(stream) :
                default;
            stream.Seek(pos, SeekOrigin.Begin);
            return value;
        }

        public static ISerializer WriteToStream<T>(this ISerializer serializer, Stream stream, T value)
        {
            serializer?.WriteToStream(stream, value, typeof(T));
            return serializer;
        }

        public static ISerializer WriteToStream(this ISerializer serializer, Stream stream, object value)
        {
            if (value is null)
            {
                return serializer;
            }

            serializer?.WriteToStream(stream, value, value.GetType());
            return serializer;
        }

        public static Stream ToStream<T>(this ISerializer serializer, T value)
        {
            return serializer?.ToStream(value, typeof(T));
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
            return serializer?.ToString(value, typeof(T));
        }

        public static T FromString<T>(this ISerializer serializer, string valueAsString)
        {
            return serializer is not null ?
                (T)serializer.FromString(valueAsString, typeof(T)) :
                default;
        }
    }
}
