using System;
using System.IO;
using System.Text;
using System.Text.Json;
using GeneratedSerializers;

namespace Uno.Extensions.Serialization
{
    /// <summary>
    /// This serializer adapter enables usage of the
    /// System.Text.Json serializer with IObjectSerializer.
    /// </summary>
    public class SystemTextJsonToObjectSerializerAdapter : IObjectSerializer
    {
        private readonly JsonSerializerOptions _serializerOptions;

        public SystemTextJsonToObjectSerializerAdapter(JsonSerializerOptions serializerOptions = null)
        {
            _serializerOptions = serializerOptions;
        }

        public object FromStream(Stream source, Type targetType)
        {
            using (var streamReader = new StreamReader(source))
            {
                var deserialized = streamReader.ReadToEnd();
                return JsonSerializer.Deserialize(deserialized, targetType, _serializerOptions);
            }
        }

        public object FromString(string source, Type targetType)
        {
            return JsonSerializer.Deserialize(source, targetType, _serializerOptions);
        }

        public bool IsSerializable(Type valueType)
        {
            return true;
        }

        public Stream ToStream(object value, Type valueType)
        {
            var memoryStream = new MemoryStream();

            using (var streamWriter = new StreamWriter(memoryStream))
            {
                var serialized = JsonSerializer.Serialize(value, valueType, _serializerOptions);

                streamWriter.Write(serialized);
                streamWriter.Flush();
                memoryStream.Seek(0, SeekOrigin.Begin);
            }

            return memoryStream;
        }

        public string ToString(object value, Type valueType)
        {
            return JsonSerializer.Serialize(value, valueType);
        }

        public void WriteToStream(object value, Type valueType, Stream stream, bool canDisposeStream = true)
        {
            if (stream is null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            using (var streamWriter = new StreamWriter(stream))
            {
                var serialized = JsonSerializer.Serialize(value, valueType, _serializerOptions);

                streamWriter.Write(serialized);
                streamWriter.Flush();
            }

            if (canDisposeStream)
            {
                stream.Dispose();
            }
        }

        public void WriteToString(object value, Type valueType, StringBuilder builder)
        {
            if (builder is null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            builder.Append(JsonSerializer.Serialize(value, valueType, _serializerOptions));
        }
    }
}
