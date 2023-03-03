using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MVUxToDos.Data;

public interface IDataStore
{
	ValueTask<int> GetIntValue(CancellationToken ct = default);


	ValueTask<string> GetPrimitiveValue(CancellationToken ct);
	ValueTask UpdatePrimitiveValue(string newPrimitiveValue, CancellationToken ct);


	ValueTask<PersonRecord> GetSinglePerson(CancellationToken ct);
	ValueTask<IImmutableList<PersonRecord>> GetPeople(CancellationToken ct);
	ValueTask<IImmutableList<PersonRecord>> GetPeople(int pageSize, int pageNumber, CancellationToken ct);
	ValueTask<int> AddPerson(PersonRecord person, CancellationToken ct);
	ValueTask UpdatePerson(PersonRecord person, CancellationToken ct);
	ValueTask DeletePerson(int personId, CancellationToken ct);

/*
	ValueTask<IImmutableList<PhoneRecord>> GetPhones(PersonRecord person, CancellationToken ct);
	ValueTask<int> AddPhone(PersonRecord person, PhoneRecord phone, CancellationToken ct);
	ValueTask UpdatePhone(PhoneRecord oldPhone, PhoneRecord newPhone, CancellationToken ct);
	ValueTask DeletePhone(int phoneId, CancellationToken ct);
*/

	ValueTask<CompanyClass> GetSingleCompany(CancellationToken ct);

}

public partial record PersonRecord(int Id, string FirstName, string LastName, IImmutableList<PhoneRecord> Phones)
{
	public PersonRecord OmitPhones() => this with { Phones = Enumerable.Empty<PhoneRecord>().ToImmutableList() };

	public static PersonRecord Empty() => new PersonRecord(default, default, default, default);

}

public partial record PhoneRecord(int Id, string Number)
{
	public static PhoneRecord Empty() => new PhoneRecord(default, default);
}

public class CompanyClass
{
	public string CompanyName { get; set; }
}
