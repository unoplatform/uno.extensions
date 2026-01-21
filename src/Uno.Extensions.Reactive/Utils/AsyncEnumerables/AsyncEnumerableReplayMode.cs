using System;
using System.Collections.Generic;
using System.Linq;

namespace Uno.Extensions.Reactive.Utils;

/// <summary>
/// Enumeration of the possible replay modes for an <see cref="IAsyncEnumerable{T}"/>
/// </summary>
public enum AsyncEnumerableReplayMode
{
	/// <summary>
	/// Does not replay any value
	/// </summary>
	Disabled,

	/// <summary>
	/// Replays the whole collection from the first item (like an async List&lt;T&gt;)
	/// </summary>
	/// <remarks>
	/// Use with caution as this will leak all value produced by the <see cref="AsyncEnumerableSubject{T}"/>.
	/// </remarks>
	Enabled,

	/// <summary>
	/// Replays the whole collection, but only for the first enumerator (like Stream, cf. remarks) 
	/// </summary>
	/// <remarks>
	/// This allow the creation of an subject which is returned to a caller and which is asynchronously fed by the callee.
	/// It ensure that the caller won't miss any values published by the callee before the caller gets the enumerator on the "streamed collection",
	/// but it also ensure that the "stream collection" won't leak all published values when it's expected that the result is being enumerated only once.
	/// </remarks>
	EnabledForFirstEnumeratorOnly
}
