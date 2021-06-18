using System;
using System.IO;
using GeneratedSerializers;

namespace Uno.Extensions.Serialization
{
    public class GeneratedStreamSerializer : IStreamSerializer
    {

        private IObjectSerializer GeneratedSerializer { get; }

        public GeneratedStreamSerializer(IObjectSerializer generatedSerializer)
        {
            GeneratedSerializer = generatedSerializer;
        }

        public object ReadFromStream(Stream source, Type targetType)
        {
            return GeneratedSerializer.FromStream(source, targetType);
        }

        public void WriteToStream(Stream stream, object value, Type valueType)
        {
            GeneratedSerializer.WriteToStream(value, valueType, stream, false);
        }

        public string ToString(object value, Type valueType)
        {
            return GeneratedSerializer.ToString(value, valueType);
        }

        public object FromString(string source, Type targetType)
        {
            return GeneratedSerializer.FromString(source, targetType);
        }
    }
}
