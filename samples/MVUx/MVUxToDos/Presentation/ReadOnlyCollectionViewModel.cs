using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using MVUxToDos.Data;
using Uno.Extensions.Reactive;

namespace MVUxToDos.Presentation;

public partial class ReadOnlyCollectionViewModel
{
	private readonly IDataStore dataStore = new DataStore { TaskDelay = TimeSpan.FromSeconds(1) };

	public IFeed<IImmutableList<PersonRecord>> People => Feed.Async(valueProvider: dataStore.GetPeople);
}
