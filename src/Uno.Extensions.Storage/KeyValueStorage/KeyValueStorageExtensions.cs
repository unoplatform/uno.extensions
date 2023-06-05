namespace Uno.Extensions.Storage.KeyValueStorage;

/// <summary>
/// Extensions for working with <see cref="IKeyValueStorage"/>.
/// </summary>
public static class KeyValueStorageExtensions
{
	/// <summary>
	/// Determines if the storage contains the specified key.
	/// </summary>
	/// <param name="storage">The storage instance</param>
	/// <param name="key">The key to search for</param>
	/// <param name="ct">CancellationToken to cancel operation</param>
	/// <returns>True if storage contains key</returns>
	public static async ValueTask<bool> ContainsKey(this IKeyValueStorage storage, string key, CancellationToken ct)
	{
		var keys = await storage.GetKeysAsync(ct);
		return keys.Contains(key);
	}

	/// <summary>
	/// Gets a value from the storage as a string
	/// </summary>
	/// <param name="storage">The storage instance</param>
	/// <param name="key">The key to retrieve</param>
	/// <param name="ct">CancellationToken to cancel operation</param>
	/// <returns>The value, or null if key not in storage</returns>
	public static async ValueTask<string?> GetStringAsync(
		this IKeyValueStorage storage,
		string key,
		CancellationToken ct)
	{
		if (await storage.ContainsKey(key, ct))
		{
			return await storage.GetAsync<string>(key, ct);
		}

		return default;
	}

	/// <summary>
	/// Retrieves all key/value pairs from the storage.
	/// </summary>
	/// <param name="storage">The storage instance</param>
	/// <param name="ct">CancellationToken to cancel operation</param>
	/// <returns>Dictionary of key-value pairs</returns>
	public static ValueTask<IDictionary<string, string>> GetAllValuesAsync(
		this IKeyValueStorage storage,
		CancellationToken ct)
			=> GetAllValuesAsync(storage, _ => true, ct);

	/// <summary>
	/// Retrieves all key/value pairs from the storage that match the predicate.
	/// </summary>
	/// <param name="storage">The storage instance</param>
	/// <param name="predicate">The predicate to invoke to determine if pair should be returned</param>
	/// <param name="ct">CancellationToken to cancel operation</param>
	/// <returns>Dictionary of key-value pairs</returns>
	public static async ValueTask<IDictionary<string, string>> GetAllValuesAsync(
		this IKeyValueStorage storage,
		Func<string, bool> predicate,
		CancellationToken ct)
	{
		var dict = new Dictionary<string, string>();
		var keys = await storage.GetKeysAsync(ct);
		foreach (var key in keys.Where(predicate))
		{
			var value = await storage.GetAsync<string>(key, ct);
			if (value is not null)
			{
				dict[key] = value;
			}
		}
		return dict;
	}

	/// <summary>
	/// Clear all values from the storage.
	/// </summary>
	/// <param name="storage">The storage instance</param>
	/// <param name="ct">CancellationToken to cancel operation</param>
	/// <returns>Task to await</returns>
	public static ValueTask ClearAllAsync(
	this IKeyValueStorage storage,
	CancellationToken ct)
		=> ClearAllAsync(storage, _ => true, ct);

	/// <summary>
	/// Clear all values from storage that match the predicate.
	/// </summary>
	/// <param name="storage">The storage instance</param>
	/// <param name="predicate">The predicate to invoke to determine if pair should be cleared</param>
	/// <param name="ct">CancellationToken to cancel operation</param>
	/// <returns>Task to await</returns>
	public static async ValueTask ClearAllAsync(
		this IKeyValueStorage storage,
		Func<string, bool> predicate,
		CancellationToken ct)
	{
		var keys = await storage.GetKeysAsync(ct);
		foreach (var key in keys.Where(predicate))
		{
			await storage.ClearAsync(key, ct);
		}
	}
}
