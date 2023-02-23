using MVUxToDos.Data;
using Uno.Extensions.Reactive;
namespace MVUxToDos.Presentation;

public partial class ReadOnlyRecordViewModel
{
	private readonly IDataStore dataStore = new DataStore();

	public IFeed<PersonRecord> PersonRecord => Feed.Async(valueProvider: dataStore.GetSinglePerson);
}
