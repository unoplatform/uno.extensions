namespace Uno.Extensions.Storage.KeyValueStorage;

public static class KeyValueStorageExtensions
{
	public static async ValueTask<bool> ContainsKey(this IKeyValueStorage storage, string key, CancellationToken ct)
	{
		var keys = await storage.GetKeysAsync(ct);
		return keys.Contains(key);
	}

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

	public static ValueTask<IDictionary<string, string>> GetAllValuesAsync(
		this IKeyValueStorage storage,
		CancellationToken ct)
	{
		return GetAllValuesAsync(storage, _ => true, ct);
	}
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

	public static ValueTask ClearAllAsync(
	this IKeyValueStorage storage,
	CancellationToken ct)
	{
		return ClearAllAsync(storage, _ => true, ct);	
	}

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
