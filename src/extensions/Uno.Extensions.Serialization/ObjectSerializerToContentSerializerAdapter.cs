using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using GeneratedSerializers;
using Refit;

namespace Uno.Extensions.Serialization
{
    /// <summary>
    /// This serializer adapter enables usage of the
    /// static serializers with Refit.
    /// </summary>
    public class ObjectSerializerToContentSerializerAdapter : IContentSerializer
    {
        private static readonly MediaTypeHeaderValue _jsonMediaType = new MediaTypeHeaderValue("application/json") { CharSet = Encoding.UTF8.WebName };

        private readonly IObjectSerializer _serializer;

        public ObjectSerializerToContentSerializerAdapter(IObjectSerializer serializer)
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        public async Task<T> DeserializeAsync<T>(HttpContent content)
        {
            if (content is null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            using (var stream = await content.ReadAsStreamAsync())
            {
                return (T)_serializer.FromStream(stream, typeof(T));
            }
        }

        public Task<HttpContent> SerializeAsync<T>(T item)
        {
            var stream = _serializer.ToStream(item, item.GetType());
            var content = new StreamContent(stream);
            content.Headers.ContentType = _jsonMediaType;

            return Task.FromResult<HttpContent>(content);
        }
    }
}
