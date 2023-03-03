using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MVUxToDos.Data;
using Uno.Extensions.Reactive;

namespace MVUxToDos.Presentation;

public partial class ReadWriteCollectionMasterDetailViewModel
{
	private readonly IDataStore dataStore = new DataStore { TaskDelay = TimeSpan.FromSeconds(1) };

	public IListState<PersonRecord> People =>
		ListState
		.Async(this, dataStore.GetPeople)
		.Selection(SelectedPerson);

	public IState<PersonRecord> NewPersonPlaceholder => State<PersonRecord>.Empty(this);
	public IState<PersonRecord> SelectedPerson => State<PersonRecord>.Empty(this);


	public IState<PhoneRecord> NewPhonePlaceholder => State<PhoneRecord>.Empty(this);


	public ReadWriteCollectionMasterDetailViewModel()
	{
		SelectedPerson.ForEachAsync(async (person, ct) =>
		{

		});
	}


	public async ValueTask CreatePerson(PersonRecord newPerson, CancellationToken ct)
	{
		var newId = await dataStore.AddPerson(newPerson, ct);
		await People.AddAsync(newPerson with { Id = newId }, ct);

		await NewPersonPlaceholder.Update(updater: savedPerson => PersonRecord.Empty(), ct);
	}

	public async ValueTask RemovePerson(PersonRecord person, CancellationToken ct)
	{
		var personId = person.Id;

		await dataStore.DeletePerson(personId, ct);

		await People.RemoveAllAsync(match: p => p.Id == personId, ct);
	}

	public async ValueTask SavePerson(PersonRecord updatedPerson, CancellationToken ct)
	{
		await dataStore.UpdatePerson(updatedPerson, ct);

		await People
			.UpdateAsync(
				match: p => p.Id == updatedPerson.Id,
				updater: oldPerson => updatedPerson,
				ct);
	}
}
