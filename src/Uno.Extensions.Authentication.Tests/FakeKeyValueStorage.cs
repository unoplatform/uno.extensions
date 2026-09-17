using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Storage.KeyValueStorage;

namespace Uno.Extensions.Authentication;

/// <summary>
/// Minimal in-memory <see cref="IKeyValueStorage"/> for exercising <see cref="MsalTokenCacheStore"/>.
/// </summary>
/// <remarks>
/// Hand-rolled rather than mocked: the surface is four methods, and a mock would pin the exact call
/// sequence the store makes instead of the observable storage state (AGENTS.md section 5).
/// <see cref="Values"/> is exposed so tests can assert on - and corrupt - the stored representation.
/// </remarks>
internal sealed class FakeKeyValueStorage : IKeyValueStorage
{
	public bool IsEncrypted { get; init; }

	/// <summary>
	/// The backing store, keyed exactly as the caller wrote it.
	/// </summary>
	public Dictionary<string, object> Values { get; } = new();

	/// <summary>
	/// Number of <see cref="SetAsync"/> calls, so tests can assert that a no-op path wrote nothing.
	/// </summary>
	public int WriteCount { get; private set; }

	public ValueTask ClearAsync(string key, CancellationToken ct)
	{
		Values.Remove(key);
		return default;
	}

	public ValueTask<TValue?> GetAsync<TValue>(string key, CancellationToken ct) =>
		new(Values.TryGetValue(key, out var value) ? (TValue)value : default);

	public ValueTask<string[]> GetKeysAsync(CancellationToken ct) =>
		new(Values.Keys.ToArray());

	public ValueTask SetAsync<TValue>(string key, TValue value, CancellationToken ct)
		where TValue : notnull
	{
		WriteCount++;
		Values[key] = value;
		return default;
	}
}
