

namespace Uno.Extensions.Storage.KeyValueStorage;

/// <summary>
/// Represents a service that can store key-value pairs.
/// </summary>
public interface IKeyValueStorage
{
	/// <summary>
	/// Gets a value indicating whether data is encrypted
	/// </summary>
	bool IsEncrypted { get; }

	/// <summary>
	/// Removes any value stored under the provided key.
	/// </summary>
	/// <param name="key">The key to clear</param>
	/// <param name="ct">A cancellation token.</param>
	/// <returns></returns>
	ValueTask ClearAsync(string key, CancellationToken ct);

	/// <summary>
	/// Gets a value saved under that name. If that value does not exist, return the default value of TValue.
	/// If an exception happens during deserialization it is swallowed and the default value of TValue returned.
	/// </summary>
	/// <typeparam name="TValue">The returned value type. This type must be serializable.</typeparam>
	/// <param name="key">The key to get the value for.</param>
	/// <param name="ct">A cancellation token.</param>
	/// <returns></returns>
	/// <remarks>When the default selector is called, this default value is not stored.</remarks>
	ValueTask<TValue?> GetAsync<TValue>(string key, CancellationToken ct);


	/// <summary>
	/// Sets the value for the specified key (overrides any existing value)
	/// </summary>
	/// <typeparam name="TValue">The updated value type. This type must be serializable.</typeparam>
	/// <param name="key">The key to save the value under.</param>
	/// <param name="value">The value to save under the provided key.</param>
	/// <param name="ct">A cancellation token.</param>
	/// <returns></returns>
	ValueTask SetAsync<TValue>(string key, TValue value, CancellationToken ct) where TValue : notnull;

	/// <summary>
	/// Gets an array of all keys that currently have a value saved under their name.
	/// </summary>
	/// <param name="ct">A cancellation token.</param>
	/// <returns></returns>
	ValueTask<string[]> GetKeysAsync(CancellationToken ct);
}
