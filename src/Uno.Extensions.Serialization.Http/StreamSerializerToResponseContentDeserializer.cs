namespace Uno.Extensions.Serialization.Http;

/// <summary>
/// An implementation of <see cref="IResponseContentDeserializer"/> that uses an <see cref="ISerializer"/> to deserialize the content.
/// </summary>
public class SerializerToResponseContentDeserializer : IResponseContentDeserializer
{
    private readonly ISerializer _objectSerializer;

    /// <summary>
    /// Creates a new <see cref="SerializerToResponseContentDeserializer"/> instance.
    /// </summary>
    /// <param name="objectSerializer">
    /// The <see cref="ISerializer"/> to use to deserialize the content.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="objectSerializer"/> is null.
    /// </exception>
    public SerializerToResponseContentDeserializer(ISerializer objectSerializer)
    {
        _objectSerializer = objectSerializer ?? throw new ArgumentNullException(nameof(objectSerializer));
    }

    /// <summary>
    /// Deserializes the content of an <see cref="HttpResponseMessage"/> to an object of type <typeparamref name="TResponse"/>.
    /// </summary>
    /// <typeparam name="TResponse">
    /// The type of the object to deserialize the content to.
    /// </typeparam>
    /// <param name="ct">
    /// A <see cref="CancellationToken"/> to use to cancel the operation.
    /// </param>
    /// <param name="content">
    /// The <see cref="HttpContent"/> to deserialize.
    /// </param>
    /// <returns>
    /// An object of type <typeparamref name="TResponse"/> deserialized from the content.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="content"/> is null.
    /// </exception>
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
