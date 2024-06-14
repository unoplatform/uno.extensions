namespace TestHarness.Ext.Navigation.Apps.Chefs.Models;

public record ChefsAppConfig
{
	public string? Title { get; init; }
	public bool? IsDark { get; init; }
	public bool? Notification { get; init; }
	public string? AccentColor { get; init; }
}
