namespace Uno.Extensions.Authentication;

internal record TokenStorageProvider(IServiceProvider Services, string Name)
{
	public IKeyValueStorage Storage => Services.GetNamedService<IKeyValueStorage>(Name)??throw new KeyNotFoundException($"No KeyValueStorage available for {Name}");
}
