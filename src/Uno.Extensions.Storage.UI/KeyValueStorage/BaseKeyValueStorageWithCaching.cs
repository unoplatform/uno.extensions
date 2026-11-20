namespace Uno.Extensions.Storage.KeyValueStorage;

internal abstract record BaseKeyValueStorageWithCaching(
	InMemoryKeyValueStorage InMemoryStorage,
	KeyValueStorageSettings settings
	) : IKeyValueStorage
{
	private readonly FastAsyncLock _lock = new FastAsyncLock();
	private IList<string>? _inMemoryKeys;

	public abstract bool IsEncrypted { get; }

	public async ValueTask ClearAsync(string key, CancellationToken ct)
	{
		using (await _lock.LockAsync(ct))
		{
			await InternalClearAsync(key, ct);
			await InMemoryStorage.ClearAsync(key, ct);
			if (_inMemoryKeys?.Contains(key)??false)
			{
				_inMemoryKeys.Remove(key);
			}
		}
	}

	protected abstract ValueTask InternalClearAsync(string key, CancellationToken ct) ;


	public async ValueTask<TValue?> GetAsync<TValue>(string key, CancellationToken ct)
	{
		using (await _lock.LockAsync(ct))
		{
			if (!settings.DisableInMemoryCache)
			{
				var val = await InMemoryStorage.GetAsync<TValue>(key, ct);
				if (val != default(TValue))
				{
					return val;
				}
			}
			var internalVal = await InternalGetAsync<TValue>(key, ct);
			if (!settings.DisableInMemoryCache &&
				internalVal is not null)
			{
#pragma warning disable CS8714 // Incorrect Nullability warning
				await InMemoryStorage.SetAsync(key, internalVal, ct);
#pragma warning restore CS8714 // The type cannot be used as type parameter in the generic type or method. Nullability of type argument doesn't match 'notnull' constraint.
			}
			return internalVal;
		}
	}

	protected abstract ValueTask<TValue?> InternalGetAsync<TValue>(string key, CancellationToken ct);

	public async ValueTask<string[]> GetKeysAsync(CancellationToken ct)
	{
		using (await _lock.LockAsync(ct))
		{
			if (!settings.DisableInMemoryCache &&
				_inMemoryKeys is not null)
			{
				return _inMemoryKeys.ToArray();
			}
			var keys = await InternalGetKeysAsync(ct);
			_inMemoryKeys = keys.ToList();
			return keys;
		}
	}
	protected abstract ValueTask<string[]> InternalGetKeysAsync(CancellationToken ct) ;

	public async ValueTask SetAsync<TValue>(string key, TValue value, CancellationToken ct) where TValue : notnull
	{
		using (await _lock.LockAsync(ct))
		{
			if (!settings.DisableInMemoryCache 
				)
			{
				if(_inMemoryKeys is not null &&
					!_inMemoryKeys.Contains(key))
				{
					_inMemoryKeys.Add(key);
				}
				await InMemoryStorage.SetAsync(key, value, ct);
			}
			await InternalSetAsync(key, value, ct);
		}
	}

	protected abstract ValueTask InternalSetAsync<TValue>(string key, TValue value, CancellationToken ct) where TValue : notnull ;
}
