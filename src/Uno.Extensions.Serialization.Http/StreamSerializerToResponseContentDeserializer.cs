using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Http;

namespace Uno.Extensions.Serialization.Http;

public class SerializerToResponseContentDeserializer : IResponseContentDeserializer
{
    private readonly ISerializer _objectSerializer;

    public SerializerToResponseContentDeserializer(ISerializer objectSerializer)
    {
        _objectSerializer = objectSerializer ?? throw new ArgumentNullException(nameof(objectSerializer));
    }

    public async Task<TResponse?> Deserialize<TResponse>(CancellationToken ct, HttpContent content)
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
