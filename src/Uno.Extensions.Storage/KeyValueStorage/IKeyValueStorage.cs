

namespace Uno.Extensions.Storage.KeyValueStorage;

/// <summary>
/// Represents a service that can store key-value pairs.
/// </summary>
public interface IKeyValueStorage
{
	/// <summary>
	/// Removes any value stored under the provided key.
	/// </summary>
	/// <param name="key">The key to clear (pass null to clear all)</param>
	/// <param name="ct">A cancellation token.</param>
	/// <returns></returns>
	ValueTask Clear(string? key, CancellationToken ct);

	/// <summary>
	/// Gets a value saved under that name. If that value does not exist, throws a <seealso cref="KeyNotFoundException"/>.
	/// </summary>
	/// <typeparam name="TValue">The returned value type. This type must be serializable.</typeparam>
	/// <param name="key">The key to get the value for.</param>
	/// <param name="ct">A cancellation token.</param>
	/// <returns></returns>
	/// <remarks>When the default selector is called, this default value is not stored.</remarks>
	ValueTask<TValue> GetValue<TValue>(string key, CancellationToken ct);


	/// <summary>
	/// Sets the value for the specified key (overrides any existing value)
	/// </summary>
	/// <typeparam name="TValue">The updated value type. This type must be serializable.</typeparam>
	/// <param name="key">The key to save the value under.</param>
	/// <param name="value">The value to save under the provided key.</param>
	/// <param name="ct">A cancellation token.</param>
	/// <returns></returns>
	ValueTask SetValue<TValue>(string key, TValue value, CancellationToken ct) where TValue : notnull;

	/// <summary>
	/// Indicates whether there's a value stored for the key.
	/// </summary>
	/// <param name="key">The key to inspect value for.</param>
	/// <param name="ct">A cancellation token.</param>
	/// <returns></returns>
	ValueTask<bool> ContainsKey(string key, CancellationToken ct);

	/// <summary>
	/// Gets an array of all keys that currently have a value saved under their name.
	/// </summary>
	/// <param name="ct">A cancellation token.</param>
	/// <returns></returns>
	ValueTask<string[]> GetAllKeys(CancellationToken ct);
}
