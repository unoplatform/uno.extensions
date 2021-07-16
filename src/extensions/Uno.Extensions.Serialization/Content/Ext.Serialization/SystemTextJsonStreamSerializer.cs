using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace Uno.Extensions.Serialization
{
    public class SystemTextJsonStreamSerializer : ISerializer
    {
        private const int DefaultBufferSize = 1024;

        private static volatile Encoding _utf8NoBOM;

        private readonly JsonSerializerOptions _serializerOptions;

        public SystemTextJsonStreamSerializer(JsonSerializerOptions serializerOptions = null)
        {
            _serializerOptions = serializerOptions;
        }

        internal static Encoding UTF8NoBOM
        {
            get
            {
                if (_utf8NoBOM == null)
                {
                    // No need for double lock - we just want to avoid extra
                    // allocations in the common case.
                    var noBOM = new UTF8Encoding(false, true);
                    Thread.MemoryBarrier();
                    _utf8NoBOM = noBOM;
                }
                return _utf8NoBOM;
            }
        }

        public object ReadFromStream(Stream source, Type targetType)
        {
            // Need to specify all parameters in order to support netstandard 2.0. Default values match the empty constructor for StreamReader
            using var streamReader = new StreamReader(source, UTF8NoBOM, true, DefaultBufferSize, leaveOpen: true);
            var deserialized = streamReader.ReadToEnd();
            return JsonSerializer.Deserialize(deserialized, targetType, _serializerOptions);
        }

        public void WriteToStream(Stream stream, object value, Type valueType)
        {
            if (stream is null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            // Need to specify all parameters in order to support netstandard 2.0. Default values match the empty constructor for StreamWriter
            using var streamWriter = new StreamWriter(stream, UTF8NoBOM, DefaultBufferSize, leaveOpen: true);
            var serialized = JsonSerializer.Serialize(value, valueType, _serializerOptions);

            streamWriter.Write(serialized);
            streamWriter.Flush();
        }

        public string ToString(object value, Type valueType)
        {
            return JsonSerializer.Serialize(value, valueType);
        }

        public object FromString(string source, Type targetType)
        {
            return JsonSerializer.Deserialize(source, targetType);
        }
    }
}
