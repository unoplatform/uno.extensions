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
		if(await storage.ContainsKey(key,ct))
		{
			return await storage.GetAsync<string>(key, ct);
		}

		return default;
	}


		public static async ValueTask<IDictionary<string,string>> GetAllValuesAsync(
		this IKeyValueStorage storage,
		CancellationToken ct)
	{
		var dict=  new Dictionary<string, string>();
		var keys = await storage.GetKeysAsync(ct);
		foreach (var key in keys)
		{
			var value = await storage.GetAsync<string>(key, ct);
			if(value is not null)
			{
				dict[key] = value;
			}
		}
		return dict;
	}

	public static async ValueTask ClearAllAsync(
		this IKeyValueStorage storage,
		CancellationToken ct)
	{
		var keys = await storage.GetKeysAsync(ct);
		foreach (var key in keys)
		{
			await storage.ClearAsync(key, ct);
		}
	}
}
