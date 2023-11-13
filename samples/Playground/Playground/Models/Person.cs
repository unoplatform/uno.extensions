using System.Text.Json.Serialization;

namespace Playground.Models;

public record Person
{
	public Person() { }

	public Person(string name, int age, double height, double weight)
	{
		Name=name;
		Age = age;
		Height = height;
		Weight=weight;
	}
	public string? Name { get; set; }

	public int Age { get; set; }
	public double  Height { get; set; }
	public double Weight { get; set; }
}

[JsonSerializable(typeof(Person))]
internal partial class PersonContext : JsonSerializerContext
{ }
