using System;
using Nventive.Persistence;

namespace Uno.Extensions.Serialization
{
    public class StreamSerializerToSettingsSerializerAdapter : ISettingsSerializer
    {
        private readonly IStreamSerializer _objectSerializer;

        public StreamSerializerToSettingsSerializerAdapter(IStreamSerializer objectSerializer)
        {
            _objectSerializer = objectSerializer ?? throw new ArgumentNullException(nameof(objectSerializer));
        }

        public object FromString(string source, Type targetType)
        {
            return _objectSerializer.FromString(source, targetType);
        }

        public string ToString(object value, Type valueType)
        {
            return _objectSerializer.ToString(value, valueType);
        }
    }
}
