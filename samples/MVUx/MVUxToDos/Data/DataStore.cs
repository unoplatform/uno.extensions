using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MVUxToDos.Data;

public class DataStore : IDataStore
{
    private string ScalarValue { get; set; } = "This is the actual scalar value";

    public async ValueTask<string> GetScalarValue(CancellationToken ct = default)
    {
        await Delay(ct);

        return ScalarValue;
    }

    public async ValueTask UpdateScalarValue(string newScalarValue, CancellationToken ct = default)
    {
        await Delay(ct);

        ScalarValue = newScalarValue;
    }




    public async ValueTask<IImmutableList<Person>> GetPeople(CancellationToken ct = default)
    {
        await Delay(ct);

        return PeopleOnly;
    }

    public async ValueTask<IImmutableList<Person>> GetPeople(int pageSize = 3, int pageNumber = 0, CancellationToken ct = default)
    {
        await Delay(ct);

        return PeopleOnly
            .Skip(pageSize)
            .Take(pageNumber)
            .ToImmutableList();
    }

    public async ValueTask UpdatePerson(Person person, CancellationToken ct = default)
    {
        await Delay(ct);
    }





    public async ValueTask<IImmutableList<Phone>> GetPhones(Person person, CancellationToken ct = default)
    {
        await Delay(ct);

        var index = IndexOf(person);
        if (index > -1)
        {
            return People[index].Phones.ToImmutableList();
        }

        return null;
    }
    public async ValueTask AddPhone(Person person, Phone phone, CancellationToken ct = default)
    {
        await Delay(ct);

        var index = IndexOf(person);
        if (index > -1)
        {
            People[index].Phones.Add(phone);
        }
    }
    public async ValueTask UpdatePhone(Phone phone, CancellationToken ct = default)
    {
        await Delay(ct);

        var person = People.FirstOrDefault(person => person.Phones.Contains(phone));
        var phoneIndex = person?.Phones.IndexOf(phone);
        if (phoneIndex > -1)
        {
            person.Phones[phoneIndex.Value] = phone;
        }
    }
    public async ValueTask DeletePhone(Phone phone, CancellationToken ct = default)
    {
        await Delay(ct);

        var person = People.FirstOrDefault(person => person.Phones.Contains(phone));
        var phoneIndex = person?.Phones.IndexOf(phone);
        if (phoneIndex > -1)
        {
            person.Phones.RemoveAt(phoneIndex.Value);
        }
    }








    /// <summary>
    /// Set how much time the various tasks here should be delayed as if processed.
    /// </summary>
    public TimeSpan TaskDelay { get; set; } = TimeSpan.FromSeconds(0.5);

    private async ValueTask Delay(CancellationToken ct = default) => await Task.Delay(TaskDelay, ct);

    /// <summary>
    /// Shave off phones.
    /// </summary>
    private IImmutableList<Person> PeopleOnly =>
        People
        .Select(person => new Person(person.FirstName, person.LastName))
        .ToImmutableList();


    private IList<Person> People { get; } = new[]
    {
        new Person("Master", "Yoda")
        {
            Phones = new[]
            {
                new Phone("001"),
                new Phone("002"),
            }
        },
        new Person("John", "Doe")
        {
            Phones = new[]
            {
                new Phone("003"),
                new Phone("004"),
            }
        },
        new Person("Eliott", "Fitzgerald")
        {
            Phones = new[]
            {
                new Phone("005"),
                new Phone("006"),
            }
        },
        new Person("Oliver", "McMahon")
        {
            Phones = new[]
            {
                new Phone("007"),
                new Phone("008"),
            }
        },
        new Person("Charlie", "Atkinson")
        {
            Phones = new[]
            {
                new Phone("009"),
                new Phone("010"),
            }
        },
        new Person("Amy", "Peterson")
        {
            Phones = new[]
            {
                new Phone("011"),
                new Phone("012"),
            }
        },
        new Person("Richard", "Neumann")
        {
            Phones = new[]
            {
                new Phone("013"),
                new Phone("014"),
            }
        },
        new Person("Mandy", "White")
        {
            Phones = new[]
            {
                new Phone("015"),
                new Phone("016"),
            }
        },
        new Person("Xavier", "Chandler")
        {
            Phones = new[]
            {
                new Phone("017"),
                new Phone("018"),
            }
        },
    };

    private int IndexOf(Person person) => PeopleOnly.IndexOf(person);


}
