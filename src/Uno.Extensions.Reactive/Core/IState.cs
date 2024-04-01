using System;
using System.ComponentModel;
using System.Linq;
using Uno.Extensions.Reactive.Core;

namespace Uno.Extensions.Reactive;

/// <summary>
/// The base interface for <see cref="IState{T}"/> and <see cref="IListState{T}"/>.
/// This should not be used unless for type constraints which matches one of the generic types.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IState : IAsyncDisposable
{
	/// <summary>
	/// The context to which this state belongs.
	/// </summary>
	/// <remarks>This context is expected to be a "root" context.</remarks>
	[EditorBrowsable(EditorBrowsableState.Never)]
	SourceContext Context { get; }

	// Note: Should a State always be only a wrapper over a feed (i.e. no interface, only concrete type!) and expose (internally?) its FeedSubscription?
	/// <summary>
	/// The request source to use to manipulate that state.
	/// </summary>
	/// <remarks>
	/// This is expected to be the request source of the context used to subscribe to the underlying feed if any.
	/// If the state implementation is not wrapping a feed (uncommon), requests sent to this should be handled by the state itself.
	/// </remarks>
	[EditorBrowsable(EditorBrowsableState.Never)]
	internal IRequestSource Requests { get; }
}
