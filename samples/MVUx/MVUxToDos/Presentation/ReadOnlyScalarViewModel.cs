using MVUxToDos.Data;
using Uno.Extensions.Reactive;

namespace MVUxToDos.Presentation;

public partial class ReadOnlyScalarViewModel
{
    private readonly IDataStore dataStore = new DataStore();

    public IFeed<string> ScalarValue => Feed.Async(valueProvider: dataStore.GetScalarValue);

    /* 
     * same as:
     * 
     * IFeed<string> ScalarValue => Feed.Async(valueProvider: async ct => await dataStore.GetScalarValue(ct));
     */
}
