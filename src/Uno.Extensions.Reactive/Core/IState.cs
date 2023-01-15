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
	/// The context to which this state belong
	/// </summary>
	[EditorBrowsable(EditorBrowsableState.Never)]
	SourceContext Context { get; }
}
