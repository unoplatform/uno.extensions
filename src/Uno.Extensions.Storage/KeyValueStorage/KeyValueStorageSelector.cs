namespace Uno.Extensions.Storage.KeyValueStorage;

public record KeyValueStorageSelector<TService>(IServiceProvider Services, string Name)
{
	public IKeyValueStorage Storage => Services.GetNamedService<IKeyValueStorage>(Name)??throw new KeyNotFoundException($"No KeyValueStorage available for {Name}");
}
