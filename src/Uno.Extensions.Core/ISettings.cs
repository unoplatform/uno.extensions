namespace Uno.Extensions;

/// <summary>
/// Simple interface for storing key-value pairs.
/// </summary>
public interface ISettings
{
	/// <summary>
	/// Gets the value associated with the specified key.
	/// </summary>
	/// <param name="key">The key of the value to get.</param>
	/// <returns>The value associated with the specified key, or null if the key does not exist.</returns>
	string? Get(string key);

	/// <summary>
	/// Sets the value associated with the specified key.
	/// </summary>
	/// <param name="key">The key of the value to set.</param>
	/// <param name="value">The value to set.</param>
	void Set(string key, string? value);

	/// <summary>
	/// Removes the value associated with the specified key.
	/// </summary>
	/// <param name="key">The key of the value to remove.</param>
	void Remove(string key);

	/// <summary>
	/// Removes all key-value pairs from the settings.
	/// </summary>
	void Clear();

	/// <summary>
	/// Gets a collection of all keys in the settings.
	/// </summary>
	IReadOnlyCollection<string> Keys { get; }
}
