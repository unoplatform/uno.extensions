namespace Uno.Extensions.Core.UI.Tests;

internal class InMemorySettings : ISettings
{
	private readonly Dictionary<string, string?> _store = new();

	public string? Get(string key) => _store.TryGetValue(key, out var value) ? value : null;

	public void Set(string key, string? value) => _store[key] = value;

	public void Remove(string key) => _store.Remove(key);

	public void Clear() => _store.Clear();

	public IReadOnlyCollection<string> Keys => _store.Keys;
}
