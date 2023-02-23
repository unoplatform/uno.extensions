using MVUxToDos.Data;
using Uno.Extensions.Reactive;

namespace MVUxToDos.Presentation;

public partial class ReadOnlyIntViewModel
{
	private readonly IDataStore dataStore = new DataStore();

	public IFeed<int> IntValue => Feed.Async(valueProvider: dataStore.GetIntValue);
}
