using System;
using MVUxToDos.Data;
using Uno.Extensions.Reactive;

namespace MVUxToDos.Presentation;

public partial class ReadOnlyScalarLoadRefreshViewModel
{
    private readonly IDataStore dataStore = new DataStore {  TaskDelay = TimeSpan.FromSeconds(2) };

    public IFeed<string> ScalarValue { get; }

    public ReadOnlyScalarLoadRefreshViewModel()
    {
        ScalarValue = Feed.Async(valueProvider: dataStore.GetScalarValue);
    }
}
