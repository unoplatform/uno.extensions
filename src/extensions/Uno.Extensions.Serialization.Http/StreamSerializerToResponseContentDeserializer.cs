using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MallardMessageHandlers;

namespace Uno.Extensions.Serialization
{
    public class StreamSerializerToResponseContentDeserializer : IResponseContentDeserializer
    {
        private readonly IStreamSerializer _objectSerializer;

        public StreamSerializerToResponseContentDeserializer(IStreamSerializer objectSerializer)
        {
            _objectSerializer = objectSerializer ?? throw new ArgumentNullException(nameof(objectSerializer));
        }

        public async Task<TResponse> Deserialize<TResponse>(CancellationToken ct, HttpContent content)
        {
            if (content is null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            using (var stream = await content.ReadAsStreamAsync())
            {
                return _objectSerializer.FromStream<TResponse>(stream);
            }
        }
    }
}
