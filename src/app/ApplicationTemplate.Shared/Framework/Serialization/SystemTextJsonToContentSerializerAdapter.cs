using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Refit;

namespace ApplicationTemplate
{
    /// <summary>
    /// This serializer adapter enables usage of the
    /// System.Text.Json serializers with Refit.
    /// </summary>
    public class SystemTextJsonToContentSerializerAdapter : IContentSerializer
    {
        private static readonly MediaTypeHeaderValue _jsonMediaType = new MediaTypeHeaderValue("application/json") { CharSet = Encoding.UTF8.WebName };

        private readonly JsonSerializerOptions _serializerOptions;

        public SystemTextJsonToContentSerializerAdapter(JsonSerializerOptions serializerOptions = null)
        {
            _serializerOptions = serializerOptions;
        }

        public async Task<T> DeserializeAsync<T>(HttpContent content)
        {
            if (content is null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            using (var stream = await content.ReadAsStreamAsync().ConfigureAwait(false))
            {
                return await JsonSerializer.DeserializeAsync<T>(stream, _serializerOptions).ConfigureAwait(false);
            }
        }

        public Task<HttpContent> SerializeAsync<T>(T item)
        {
            var json = JsonSerializer.Serialize(item, _serializerOptions);
            var content = new StringContent(json);
            content.Headers.ContentType = _jsonMediaType;

            return Task.FromResult<HttpContent>(content);
        }
    }
}
