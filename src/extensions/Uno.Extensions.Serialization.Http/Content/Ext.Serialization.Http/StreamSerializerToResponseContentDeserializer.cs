﻿using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Http.Handlers;

namespace Uno.Extensions.Serialization.Http
{
    public class StreamSerializerToResponseContentDeserializer : IResponseContentDeserializer
    {
        private readonly ISerializer _objectSerializer;

        public StreamSerializerToResponseContentDeserializer(ISerializer objectSerializer)
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
