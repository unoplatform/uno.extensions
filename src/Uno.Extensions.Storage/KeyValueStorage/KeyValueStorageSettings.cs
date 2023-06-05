namespace Uno.Extensions.Storage.KeyValueStorage;

/// <summary>
/// Record for storing settings for a key value storage provider.
/// </summary>
public record KeyValueStorageSettings
{
	/// <summary>
	/// Gets or sets whether in-memory cache should be disabled.
	/// </summary>
	public bool DisableInMemoryCache { get; init; }
}
