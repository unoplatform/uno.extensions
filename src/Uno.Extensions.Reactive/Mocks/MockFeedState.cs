namespace Uno.Extensions.Reactive.Mocks;

/// <summary>
/// The state in which a mocked feed is pinned, for data-driving a mock from a state picker.
/// </summary>
public enum MockFeedState
{
	/// <summary>No data emitted yet.</summary>
	Undefined,

	/// <summary>Loading, indefinitely.</summary>
	Loading,

	/// <summary>No result (<see cref="Option{T}.None"/>).</summary>
	Empty,

	/// <summary>Data present.</summary>
	Value,

	/// <summary>Error.</summary>
	Error,

	/// <summary>Stale data present while refreshing, indefinitely.</summary>
	Refreshing,
}
