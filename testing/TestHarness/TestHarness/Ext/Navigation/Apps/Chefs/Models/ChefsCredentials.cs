namespace TestHarness.Ext.Navigation.Apps.Chefs.Models;

public record ChefsCredentials
{
	public string? Username { get; init; }
	public string? Password { get; init; }
	public bool SkipWelcome { get; init; }
	public bool SaveCredentials { get; init; }
}

