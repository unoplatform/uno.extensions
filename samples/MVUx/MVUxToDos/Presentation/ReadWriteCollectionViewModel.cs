using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MVUxToDos.Data;
using Uno.Extensions.Reactive;

namespace MVUxToDos.Presentation;

public partial class ReadWriteCollectionViewModel
{
	private readonly IDataStore dataStore = new DataStore { TaskDelay = TimeSpan.FromSeconds(1) };

	public IListState<PersonRecord> People => ListState.Async(this, dataStore.GetPeople);


	public async ValueTask Remove(PersonRecord person, CancellationToken ct)
	{
		await dataStore.DeletePerson(person, ct);

		await People.UpdateData(
			updater: old => old.SomeOrDefault().Remove(person, EqualityComparer<PersonRecord>.Default),
		 	ct);

	}
}
