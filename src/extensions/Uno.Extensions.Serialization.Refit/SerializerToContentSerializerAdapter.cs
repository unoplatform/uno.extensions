using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Refit;

namespace Uno.Extensions.Serialization.Refit
{
    /// <summary>
    /// This serializer adapter enables usage of the
    /// static serializers with Refit.
    /// </summary>
    public class SerializerToContentSerializerAdapter : IHttpContentSerializer
    {
        private static readonly MediaTypeHeaderValue _jsonMediaType = new ("application/json") { CharSet = Encoding.UTF8.WebName };

        private readonly ISerializer _serializer;

        public SerializerToContentSerializerAdapter(ISerializer serializer)
        {
            _serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        public async Task<T> DeserializeAsync<T>(HttpContent content)
        {
            if (content is null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            using var stream = await content.ReadAsStreamAsync();
            return _serializer.FromStream<T>(stream);
        }

        public async Task<T> FromHttpContentAsync<T>(HttpContent content, CancellationToken cancellationToken = default)
        {
            if (content is null)
            {
                throw new ArgumentNullException(nameof(content));
            }

#if WINUI
            using var stream = await content.ReadAsStreamAsync(cancellationToken);
#else
            using var stream = await content.ReadAsStreamAsync();
#endif
            return _serializer.ReadFromStream<T>(stream);
        }

        public string GetFieldNameForProperty(PropertyInfo propertyInfo)
        {
            return propertyInfo?.Name;
        }

        public HttpContent ToHttpContent<T>(T item)
        {
            var stream = _serializer.ToStream(item, item?.GetType());
            var content = new StreamContent(stream);
            content.Headers.ContentType = _jsonMediaType;

            return content;
        }
    }
}
