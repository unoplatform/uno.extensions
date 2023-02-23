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
	ValueTask AddPerson(PersonRecord person, CancellationToken ct);
	ValueTask UpdatePerson(PersonRecord person, CancellationToken ct);
	ValueTask DeletePerson(PersonRecord person, CancellationToken ct);


	ValueTask<IImmutableList<PhoneRecord>> GetPhones(PersonRecord person, CancellationToken ct);
	ValueTask AddPhone(PersonRecord person, PhoneRecord phone, CancellationToken ct);
	ValueTask UpdatePhone(PhoneRecord phone, CancellationToken ct);
	ValueTask DeletePhone(PhoneRecord phone, CancellationToken ct);

	ValueTask<CompanyClass> GetSingleCompany(CancellationToken ct);

}

public record PersonRecord(string FirstName, string LastName, IImmutableList<PhoneRecord> Phones)
{
	public PersonRecord WithoutPhone() => new(FirstName, LastName, Enumerable.Empty<PhoneRecord>().ToImmutableList());
}

public record PhoneRecord(string Number)
{	
}

public class CompanyClass
{
	public string CompanyName { get; set; }
}
