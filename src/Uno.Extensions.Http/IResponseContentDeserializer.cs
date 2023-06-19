namespace Uno.Extensions.Http;

/// <summary>
/// Allows the deserialization of the response content.
/// </summary>
public interface IResponseContentDeserializer
{
	/// <summary>
	/// Deserializes the response into the.
	/// </summary>
	/// <typeparam name="TResponse">The type of response.</typeparam>
	/// <param name="ct">Cancel token.</param>
	/// <param name="content">Http content.</param>
	/// <returns>Http response.</returns>
	Task<TResponse?> Deserialize<TResponse>(CancellationToken ct, HttpContent content);
}
