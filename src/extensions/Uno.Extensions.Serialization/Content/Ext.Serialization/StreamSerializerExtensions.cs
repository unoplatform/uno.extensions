using System;
using System.IO;

namespace Uno.Extensions.Serialization
{
    public static class StreamSerializerExtensions
    {

        public static T ReadFromStream<T>(this IStreamSerializer serializer, Stream stream)
        {
            return (T)serializer.ReadFromStream(stream, typeof(T));
        }

        public static T FromStream<T>(this IStreamSerializer serializer, Stream stream)
        {
            var pos = stream.Position;
            var value = (T)serializer.ReadFromStream<T>(stream);
            stream.Seek(pos, SeekOrigin.Begin);
            return value;
        }

        public static IStreamSerializer WriteToStream<T>(this IStreamSerializer serializer, Stream stream, T value)
        {
            serializer.WriteToStream(stream, value, typeof(T));
            return serializer;
        }

        public static IStreamSerializer WriteToStream(this IStreamSerializer serializer, Stream stream, object value)
        {
            serializer.WriteToStream(stream, value, value.GetType());
            return serializer;
        }

        public static Stream ToStream<T>(this IStreamSerializer serializer, T value)
        {
            return serializer.ToStream(value, typeof(T));
        }

        public static Stream ToStream(this IStreamSerializer serializer, object value, Type valueType)
        {
            var memoryStream = new MemoryStream();

            serializer.WriteToStream(memoryStream, value, valueType);
            memoryStream.Seek(0, SeekOrigin.Begin);

            return memoryStream;
        }

        public static string ToString<T>(this IStreamSerializer serializer, T value)
        {
            return serializer.ToString(value, typeof(T));
        }

        public static T FromString<T>(this IStreamSerializer serializer, string valueAsString)
        {
            return (T)serializer.FromString(valueAsString, typeof(T));
        }

    }
}
