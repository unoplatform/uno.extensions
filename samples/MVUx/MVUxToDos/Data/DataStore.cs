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

		return PeopleOnly.FirstOrDefault();
	}

	public async ValueTask<IImmutableList<PersonRecord>> GetPeople(CancellationToken ct = default)
	{
		await Delay(ct);

		return PeopleOnly;
	}

	public async ValueTask<IImmutableList<PersonRecord>> GetPeople(int pageSize = 3, int pageNumber = 0, CancellationToken ct = default)
	{
		await Delay(ct);

		return PeopleOnly
			.Skip(pageSize)
			.Take(pageNumber)
			.ToImmutableList();
	}

	public async ValueTask AddPerson(PersonRecord person, CancellationToken ct = default)
	{
		await Delay(ct);

		var index = IndexOf(person);
		if (index == -1)
		{
			People.Add(person);
		}
		else
		{
			throw new InvalidOperationException($"{person} already exists.");
		}
	}

	public async ValueTask UpdatePerson(PersonRecord person, CancellationToken ct = default)
	{
		await Delay(ct);

		// TODO avoid dupes
		var index = IndexOf(person);
		People[index] = person;
	}

	public async ValueTask DeletePerson(PersonRecord person, CancellationToken ct = default)
	{
		await Delay(ct);

		var index = IndexOf(person);
		if (index > -1)
		{
			People.RemoveAt(index);
		}
	}





	public async ValueTask<IImmutableList<PhoneRecord>> GetPhones(PersonRecord person, CancellationToken ct = default)
	{
		await Delay(ct);

		var index = IndexOf(person);
		if (index > -1)
		{
			return People[index].Phones.ToImmutableList();
		}

		return null;
	}
	public async ValueTask AddPhone(PersonRecord person, PhoneRecord phone, CancellationToken ct = default)
	{
		await Delay(ct);

		var index = IndexOf(person);
		if (index > -1)
		{
			var newPerson =
				person with
				{
					Phones = People[index].Phones.Add(phone)
				};
			People[index] = newPerson;
		}
	}
	public async ValueTask UpdatePhone(PhoneRecord phone, CancellationToken ct = default)
	{
		await Delay(ct);

		var person = People.FirstOrDefault(person => person.Phones.Contains(phone));
		var personIndex = IndexOf(person);
		var phoneIndex = person?.Phones.IndexOf(phone) ?? -1;
		if (phoneIndex > -1)
		{
			var newPerson =
				person with
				{
					Phones = person.Phones.RemoveAt(phoneIndex)
				};
			People[personIndex] = newPerson;
		}
	}

	public async ValueTask DeletePhone(PhoneRecord phone, CancellationToken ct = default)
	{
		await Delay(ct);

		var person = People.FirstOrDefault(person => person.Phones.Contains(phone));
		var personIndex = IndexOf(person);
		var phoneIndex = person?.Phones.IndexOf(phone);
		if (phoneIndex > -1)
		{
			var newPerson =
				person with
				{
					Phones = person.Phones.RemoveAt(phoneIndex.Value)
				};
			People[personIndex] = newPerson;
		}
	}



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

	/// <summary>
	/// Shave off phones.
	/// </summary>
	private IImmutableList<PersonRecord> PeopleOnly =>
		People
		.Select(person => new PersonRecord(person.FirstName, person.LastName, EmptyPhones()))
		.ToImmutableList();


	private IList<PersonRecord> People { get; } = new List<PersonRecord>
	{
		new PersonRecord("Master", "Yoda", new[]
			{
				new PhoneRecord("001"),
				new PhoneRecord("002"),
			}.ToImmutableList()),

		new PersonRecord("John", "Doe", new[]
			{
				new PhoneRecord("003"),
				new PhoneRecord("004"),
			}.ToImmutableList()),

		new PersonRecord("Eliott", "Fitzgerald", new[]
			{
				new PhoneRecord("005"),
				new PhoneRecord("006"),
			}.ToImmutableList()),

		new PersonRecord("Oliver", "McMahon", new[]
			{
				new PhoneRecord("007"),
				new PhoneRecord("008"),
			}.ToImmutableList()),

		new PersonRecord("Charlie", "Atkinson", new[]
			{
				new PhoneRecord("009"),
				new PhoneRecord("010"),
			}.ToImmutableList()),

		new PersonRecord("Amy", "Peterson", new[]
			{
				new PhoneRecord("011"),
				new PhoneRecord("012"),
			}.ToImmutableList()),

		new PersonRecord("Richard", "Neumann", new[]
			{
				new PhoneRecord("013"),
				new PhoneRecord("014"),
			}.ToImmutableList()),

		new PersonRecord("Mandy", "White", new[]
			{
				new PhoneRecord("015"),
				new PhoneRecord("016"),
			}.ToImmutableList()),

		new PersonRecord("Xavier", "Chandler", new[]
			{
				new PhoneRecord("017"),
				new PhoneRecord("018"),
			}.ToImmutableList())
	};



	private IList<CompanyClass> Companies { get; } = new[]
	{
		new CompanyClass { CompanyName = "Uno Platform" },
		new CompanyClass { CompanyName = "NVentive" }
	};

	private IImmutableList<PhoneRecord> EmptyPhones() => Enumerable.Empty<PhoneRecord>().ToImmutableList();

	private int IndexOf(PersonRecord person) => PeopleOnly.IndexOf(person);


}
