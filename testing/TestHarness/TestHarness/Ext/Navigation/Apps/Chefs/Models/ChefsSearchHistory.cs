namespace TestHarness.Ext.Navigation.Apps.Chefs.Models;

public record ChefsSearchHistory
{
	public List<string> Searches { get; init; } = new();
}
