namespace Playground.Models;

/// <summary>
/// A XAML-friendly FeedView mock envelope, exposing exactly the members recognized by the
/// FeedView mock source convention (Data / Progress / Error) so the FeedView can render
/// each of its states from design-time data.
/// </summary>
public class FeedMock
{
	/// <summary>The mocked value: non-null renders the value template, null renders the None state.</summary>
	public object? Data { get; set; }

	/// <summary>When true, the FeedView renders its progress (loading) state.</summary>
	public bool Progress { get; set; }

	/// <summary>The mocked error: an Exception, or any value (e.g. a string) used as the error message.</summary>
	public object? Error { get; set; }
}
