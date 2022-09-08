namespace Uno.Extensions.Storage.KeyValueStorage;

internal record KeyValueStorageIndex(string MostSecure, params (string Name, bool IsEncrypted)[] AvailableStorage)
{
}
