using Uno.Extensions.Storage;
using Uno.Extensions.Storage.KeyValueStorage;

namespace TestHarness.Ext.Navigation.Storage;

public class StorageOneViewModel 
{
	public StorageOneViewModel(INavigator Navigator, IServiceProvider Services)
	{
		var storage = Services.GetNamedService<IKeyValueStorage>("InMemory");
	}
}
