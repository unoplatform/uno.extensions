using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MVUxToDos.Data;

public class DataStore : IDataStore
{
	private string PrimitiveValue { get; set; } = "This is the actual primitive value (string)";


	public async ValueTask<int> GetIntValue(CancellationToken ct = default)
	{
		await Delay(ct);

		return 987654321;
	}

	public async ValueTask<string> GetPrimitiveValue(CancellationToken ct = default)
	{
		await Delay(ct);

		return PrimitiveValue;
	}

	public async ValueTask UpdatePrimitiveValue(string newPrimitiveValue, CancellationToken ct = default)
	{
		await Delay(ct);

		PrimitiveValue = newPrimitiveValue;
	}




	public async ValueTask<PersonRecord> GetSinglePerson(CancellationToken ct = default)
	{
		await Delay(ct);

		return People.FirstOrDefault();
	}

	public async ValueTask<IImmutableList<PersonRecord>> GetPeople(CancellationToken ct = default)
	{
		await Delay(ct);

		return People.ToImmutableList();
	}

	public async ValueTask<IImmutableList<PersonRecord>> GetPeople(int pageSize = 3, int pageNumber = 0, CancellationToken ct = default)
	{
		await Delay(ct);

		return People
			.Skip(pageSize)
			.Take(pageNumber)
			.ToImmutableList();
	}

	public async ValueTask<int> AddPerson(PersonRecord person, CancellationToken ct = default)
	{
		await Delay(ct);

		var newId = People.Max(person => person.Id) + 1;

		People.Add(person with { Id = newId });

		return newId;
	}

	public async ValueTask UpdatePerson(PersonRecord person, CancellationToken ct = default)
	{
		await Delay(ct);

		var (personId, index) =
			People
			.Select((p, index) => (PersonId: p.Id, Index: index))
			.Where(x => x.PersonId == person.Id)
			.SingleOrDefault(defaultValue: (PersonId: 0, Index: -1));

		if (index > -1)
		{
			People[index] = person;
		}
	}

	public async ValueTask DeletePerson(int personId, CancellationToken ct = default)
	{
		await Delay(ct);

		var person = People.SingleOrDefault(person => person.Id == personId);
		if (person != null)
		{
			People.Remove(person);
		}
	}


	/*
	public async ValueTask<IImmutableList<PhoneRecord>> GetPhones(PersonRecord person, CancellationToken ct = default)
	{
		await Delay(ct);

		var index = GetPersonIndex(person);
		if (index > -1)
		{
			return People[index].Phones;
		}

		return null;
	}

	public async ValueTask<int> AddPhone(PersonRecord person, PhoneRecord phone, CancellationToken ct = default)
	{
		await Delay(ct);

		var index = GetPersonIndex(person);
		if (index < 0)
		{
			return -1;
		}

		var newId = People
			.SelectMany(person => person.Phones)
			.Select(phone => phone.Id)
			.Max() + 1;

		phone = phone with { Id = newId };

		var newPerson =
				person with
				{
					Phones = People[index].Phones.Add(phone)
				};

		People[index] = newPerson;

		return newId;
	}

	public async ValueTask UpdatePhone(PhoneRecord oldPhone, PhoneRecord newPhone, CancellationToken ct = default)
	{
		await Delay(ct);

		(var person, var personIndex) =
			People
			.Where(person =>
				person.Phones.Any(phone => phone.Id == oldPhone.Id))
			.Select((person, index) => (person, index))
			.SingleOrDefault(defaultValue: (default, -1));

		(var phone, var phoneIndex) =
			person.Phones
			.Where(phone => phone.Id == oldPhone.Id)
			.Select((phone, index) => (phone, index))
			.SingleOrDefault(defaultValue: (default, -1));

		if (phoneIndex < 0)
		{
			return;
		}

		var newPerson =
				person with
				{
					Phones =
						person.Phones
						.RemoveAt(phoneIndex)
						.Insert(phoneIndex, newPhone)
				};

		People[personIndex] = newPerson;
	}

	public async ValueTask DeletePhone(int phoneId, CancellationToken ct = default)
	{
		await Delay(ct);

		(var person, var personIndex) =
			People
			.Where(person =>
				person.Phones.Any(phone => phone.Id == phoneId))
			.Select((person, index) => (person, index))
			.SingleOrDefault(defaultValue: (default, -1));

		var phoneIndex =
			person.Phones
			.Where(phone => phone.Id == phoneId)
			.Select((phone, index) => index)
			.SingleOrDefault(defaultValue: -1);

		if (phoneIndex < 0)
		{
			return;
		}

		var newPerson =
				person with
				{
					Phones = person.Phones.RemoveAt(phoneIndex)
				};

		People[personIndex] = newPerson;
	}
	*/


	public async ValueTask<CompanyClass> GetSingleCompany(CancellationToken ct)
	{
		await Delay(ct);

		return Companies.FirstOrDefault();
	}





	/// <summary>
	/// Set how much time the various tasks here should be delayed as if processed.
	/// </summary>
	public TimeSpan TaskDelay { get; set; } = TimeSpan.FromSeconds(0.5);

	private async ValueTask Delay(CancellationToken ct = default) => await Task.Delay(TaskDelay, ct);

	private IList<PersonRecord> People { get; } = new List<PersonRecord>
	{
		new PersonRecord(1, "Master", "Yoda", new[]
			{
				new PhoneRecord(1, "001"),
				new PhoneRecord(2, "002"),
			}.ToImmutableList()),

		new PersonRecord(2, "John", "Doe", new[]
			{
				new PhoneRecord(3, "003"),
				new PhoneRecord(4, "004"),
			}.ToImmutableList()),

		new PersonRecord(3, "Eliott", "Fitzgerald", new[]
			{
				new PhoneRecord(5, "005"),
				new PhoneRecord(6, "006"),
			}.ToImmutableList()),

		new PersonRecord(4, "Oliver", "McMahon", new[]
			{
				new PhoneRecord(7, "007"),
				new PhoneRecord(8, "008"),
			}.ToImmutableList()),

		new PersonRecord(5, "Charlie", "Atkinson", new[]
			{
				new PhoneRecord(9, "009"),
				new PhoneRecord(10, "010"),
			}.ToImmutableList()),

		new PersonRecord(6, "Amy", "Peterson", new[]
			{
				new PhoneRecord(11, "011"),
				new PhoneRecord(12, "012"),
			}.ToImmutableList()),

		new PersonRecord(7, "Richard", "Neumann", new[]
			{
				new PhoneRecord(13, "013"),
				new PhoneRecord(14, "014"),
			}.ToImmutableList()),

		new PersonRecord(8, "Mandy", "White", new[]
			{
				new PhoneRecord(15, "015"),
				new PhoneRecord(16, "016"),
			}.ToImmutableList()),

		new PersonRecord(9, "Xavier", "Chandler", new[]
			{
				new PhoneRecord(17, "017"),
				new PhoneRecord(18, "018"),
			}.ToImmutableList())
	};

	private IList<CompanyClass> Companies { get; } = new[]
	{
		new CompanyClass { CompanyName = "Uno Platform" },
		new CompanyClass { CompanyName = "NVentive" }
	};


	private int GetPersonIndex(PersonRecord person) =>
		People
			.Where(existing => existing.Id == person.Id)
			.Select((existing, index) => index)
			.SingleOrDefault();


}
