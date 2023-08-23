
namespace Uno.Extensions.Serialization.Refit;

/// <summary>
/// This serializer adapter enables usage of the
/// static serializers with Refit.
/// </summary>
public class SerializerToContentSerializerAdapter : IHttpContentSerializer
{
	private static readonly MediaTypeHeaderValue _jsonMediaType = new("application/json") { CharSet = Encoding.UTF8.WebName };

	private readonly ISerializer _serializer;

	/// <summary>
	/// Creates a new <see cref="SerializerToContentSerializerAdapter"/> instance.
	/// </summary>
	/// <param name="serializer">
	/// The <see cref="ISerializer"/> to use to serialize and deserialize objects.
	/// </param>
	/// <exception cref="ArgumentNullException">
	/// Thrown if <paramref name="serializer"/> is null.
	/// </exception>
	public SerializerToContentSerializerAdapter(ISerializer serializer)
	{
		_serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
	}

	/// <summary>
	/// Deserializes the content of an <see cref="HttpResponseMessage"/> to an object of type <typeparamref name="T"/>.
	/// </summary>
	/// <typeparam name="T">
	/// The type of the object to deserialize the content to.
	/// </typeparam>
	/// <param name="content">
	/// The <see cref="HttpContent"/> to deserialize.
	/// </param>
	/// <returns>
	/// An object of type <typeparamref name="T"/> deserialized from the content.
	/// </returns>
	/// <exception cref="ArgumentNullException">
	/// Thrown if <paramref name="content"/> is null.
	/// </exception>
	public async Task<T?> DeserializeAsync<T>(HttpContent content)
	{
		if (content is null)
		{
			throw new ArgumentNullException(nameof(content));
		}

		using var stream = await content.ReadAsStreamAsync();
		return _serializer.FromStream<T>(stream);
	}

	/// <summary>
	/// Deserializes the content of an <see cref="HttpResponseMessage"/> to an object of type <typeparamref name="T"/>.
	/// </summary>
	/// <typeparam name="T">
	/// The type of the object to deserialize the content to.
	/// </typeparam>
	/// <param name="content">
	/// The <see cref="HttpContent"/> to deserialize.
	/// </param>
	/// <param name="cancellationToken">
	/// A <see cref="CancellationToken"/> to use to cancel the operation.
	/// </param>
	/// <returns>
	/// An object of type <typeparamref name="T"/> deserialized from the content.
	/// </returns>
	/// <exception cref="ArgumentNullException">
	/// Thrown if <paramref name="content"/> is null.
	/// </exception>
	public async Task<T?> FromHttpContentAsync<T>(HttpContent content, CancellationToken cancellationToken = default)
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
		return _serializer.FromStream<T>(stream);
	}

	/// <summary>
	/// Uses reflection to get the name of the field for a property.
	/// </summary>
	/// <param name="propertyInfo">
	/// The <see cref="PropertyInfo"/> to get the field name for.
	/// </param>
	/// <returns>
	/// The name of the field equivalent to the metadata of a property represented by <paramref name="propertyInfo"/>.
	/// </returns>
	public string GetFieldNameForProperty(PropertyInfo propertyInfo)
	{
		return propertyInfo.Name;
	}

	/// <summary>
	/// Serializes an object to an <see cref="HttpContent"/>.
	/// </summary>
	/// <typeparam name="T">
	/// The type of the object to serialize.
	/// </typeparam>
	/// <param name="item">
	/// The object to serialize.
	/// </param>
	/// <returns>
	/// A <see cref="HttpContent"/> instance representing the serialized object of type <typeparamref name="T"/>.
	/// </returns>
	public HttpContent ToHttpContent<T>(T item)
	{
		if (item is null)
		{
			return new StringContent(string.Empty);
		}
		var stream = _serializer.ToStream(item, item.GetType());
		var content = new StreamContent(stream);
		content.Headers.ContentType = _jsonMediaType;

		return content;
	}
}
