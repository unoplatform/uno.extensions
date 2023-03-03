using System;
using System.Threading;
using System.Threading.Tasks;
using MVUxToDos.Data;
using Uno.Extensions.Reactive;

namespace MVUxToDos.Presentation;

public partial class ReadWriteCollectionViewModel
{
	private readonly IDataStore dataStore = new DataStore { TaskDelay = TimeSpan.FromSeconds(1) };

	public IListState<PersonRecord> People => ListState.Async(this, dataStore.GetPeople);

	public IState<PersonRecord> NewPerson => State.Value(this, PersonRecord.Empty);

	public async ValueTask Create(PersonRecord newPerson, CancellationToken ct)
	{
		var newId = await dataStore.AddPerson(newPerson, ct);

		await People.AddAsync(newPerson with { Id = newId }, ct);

		await NewPerson.Update(old => PersonRecord.Empty(), ct);
	}


	public async ValueTask Remove(PersonRecord person, CancellationToken ct)
	{
		await dataStore.DeletePerson(person.Id, ct);

		await People.RemoveAllAsync(p => p.Id == person.Id, ct);
	}
}
