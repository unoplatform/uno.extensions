using System;

namespace Uno.Extensions.Reactive.Sources;

/// <summary>
/// The exception surfaced by a <see cref="MockFeed"/> when the mock envelope's error member
/// is not an <see cref="Exception"/> instance (e.g. an error message string coming from JSON).
/// </summary>
internal sealed class MockFeedException : Exception
{
	public MockFeedException(string message)
		: base(message)
	{
	}
}
