
namespace Uno.Extensions.Authentication;

/// <summary>
/// Implemented by classes that represent a cache for authentication tokens.
/// </summary>
public interface ITokenCache
{
	/// <summary>
	/// Gets the current provider which is associated with the tokens.
	/// </summary>
	/// <param name="ct">
	/// A <see cref="CancellationToken"/> which can be used to cancel the operation.
	/// </param>
	/// <returns>
	/// A task that represents the asynchronous operation. The task result is the name of the current provider or null.
	/// </returns>
	ValueTask<string?> GetCurrentProviderAsync(CancellationToken ct);

	/// <summary>
	/// Gets a value indicating whether the cache has tokens for the current provider.
	/// </summary>
	/// <param name="cancellationToken">
	/// A <see cref="CancellationToken"/> which can be used to cancel the operation.
	/// </param>
	/// <returns>
	/// A task that represents the asynchronous operation. The task result is true if the cache has tokens for the current provider.
	/// </returns>
	ValueTask<bool> HasTokenAsync(CancellationToken cancellationToken);

	/// <summary>
	/// Gets the tokens for the current provider.
	/// </summary>
	/// <param name="cancellationToken">
	/// A <see cref="CancellationToken"/> which can be used to cancel the operation.
	/// </param>
	/// <returns>
	/// A task that represents the asynchronous operation. The task result is the tokens for the current provider.
	/// </returns>
	ValueTask<IDictionary<string, string>> GetAsync(CancellationToken cancellationToken);

	/// <summary>
	/// Saves a dictionary of tokens for the specified provider.
	/// </summary>
	/// <param name="provider">
	/// The name of the provider for which the tokens will be saved.
	/// </param>
	/// <param name="tokens">
	/// A dictionary of tokens to save.
	/// </param>
	/// <param name="cancellationToken">
	/// A <see cref="CancellationToken"/> which can be used to cancel the operation.
	/// </param>
	/// <returns>
	/// A task that represents the asynchronous operation.
	/// </returns>
	ValueTask SaveAsync(string provider, IDictionary<string, string>? tokens, CancellationToken cancellationToken);

	/// <summary>
	/// Clears the tokens for the current provider.
	/// </summary>
	/// <param name="cancellationToken">
	/// A <see cref="CancellationToken"/> which can be used to cancel the operation.
	/// </param>
	/// <returns>
	/// A task that represents the asynchronous operation.
	/// </returns>
	ValueTask ClearAsync(CancellationToken cancellationToken);

	/// <summary>
	/// Occurs when the tokens for the current provider have changed substantially.
	/// </summary>
	event EventHandler? Cleared;
}
