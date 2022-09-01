namespace Uno.Extensions.Storage.KeyValueStorage;

internal record KeyValueStorageIndex(string MostSecureAvailableStorage, params (string Name, bool IsEncrypted)[] AvailableStorage)
{
}
