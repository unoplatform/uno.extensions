using System;
using MVUxToDos.Data;
using Uno.Extensions.Reactive;

namespace MVUxToDos.Presentation;

public partial class ReadOnlyPrimitiveWAttributeLoadRefreshViewModel
{
    private readonly IDataStore dataStore = new DataStore {  TaskDelay = TimeSpan.FromSeconds(2) };

	// This attribute is required, because an IFeed is not generated automatically for IFeed<T> when T is a primitive value or string.
	// Unless explicitly opted-in with this attribute
	[ReactiveBindable]
    public IFeed<string> PrimitiveValue { get; }

    public ReadOnlyPrimitiveWAttributeLoadRefreshViewModel()
    {
        PrimitiveValue = Feed.Async(valueProvider: dataStore.GetPrimitiveValue);
    }
}
