namespace Playground.Models;

public record Widget
{
	public Widget() { }

	public Widget(string? name, double weight)
	{
		Name=name;
		Weight=weight;
	}
	public string? Name { get; init; }

	public double Weight { get; init; }
}
