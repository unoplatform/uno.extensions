using MVUxToDos.Data;
using Uno.Extensions.Reactive;

namespace MVUxToDos.Presentation;

public partial class ReadOnlyPrimitiveViewModel
{
    private readonly IDataStore dataStore = new DataStore();

    public IFeed<string> PrimitiveValue => Feed.Async(valueProvider: dataStore.GetPrimitiveValue);

    /* 
     * same as:
     * 
     * IFeed<string> PrimitiveValue => Feed.Async(valueProvider: async ct => await dataStore.GetPrimitiveValue(ct));
     */
}
