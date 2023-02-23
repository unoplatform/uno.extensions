using MVUxToDos.Data;
using Uno.Extensions.Reactive;

namespace MVUxToDos.Presentation;

public partial class ReadOnlyClassLoadRefreshViewModel
{
	private readonly IDataStore dataStore = new DataStore();

	public IFeed<CompanyClass> CompanyClass => Feed.Async(valueProvider: dataStore.GetSingleCompany);
}
