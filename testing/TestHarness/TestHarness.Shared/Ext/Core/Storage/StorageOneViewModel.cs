using Uno.Extensions.Storage;

namespace TestHarness.Ext.Navigation.Storage;

public class StorageOneViewModel 
{
	public StorageOneViewModel(INavigator Navigator, IServiceProvider Services)
	{
		var storage = Services.GetNamedService<IKeyedStorage>("InMemory");
	}
}
