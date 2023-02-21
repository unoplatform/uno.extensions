using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;

namespace MVUxToDos.Data;

public interface IDataStore
{
    ValueTask<string> GetScalarValue(CancellationToken ct);
    ValueTask UpdateScalarValue(string newScalarValue, CancellationToken ct);


    ValueTask<IImmutableList<Person>> GetPeople(CancellationToken ct);
    ValueTask<IImmutableList<Person>> GetPeople(int pageSize, int pageNumber, CancellationToken ct);
    ValueTask UpdatePerson(Person person, CancellationToken ct);


    ValueTask<IImmutableList<Phone>> GetPhones(Person person, CancellationToken ct);
    ValueTask AddPhone(Person person, Phone phone, CancellationToken ct);
    ValueTask UpdatePhone(Phone phone, CancellationToken ct);
    ValueTask DeletePhone(Phone phone, CancellationToken ct);
}

public record Person(string FirstName, string LastName)
{
    public IList<Phone> Phones { get; set; } = new List<Phone>();
}

public record Phone(string Number);