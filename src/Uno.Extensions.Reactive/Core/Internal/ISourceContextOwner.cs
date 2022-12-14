using System;
using System.Linq;

namespace Uno.Extensions.Reactive.Core;

/// <summary>
/// Information about the owner of a <see cref="SourceContext"/>.
/// </summary>
internal interface ISourceContextOwner
{
	/// <summary>
	/// Name of the subscriber, for debug purposes
	/// </summary>
	public string? Name { get; }

	/// <summary>
	/// Get the UI thread to which this subscriber is associated, if any.
	/// </summary>
	public IDispatcher? Dispatcher { get; }
}
