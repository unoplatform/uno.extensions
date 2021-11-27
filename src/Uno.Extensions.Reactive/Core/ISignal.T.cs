using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using Uno.Extensions.Reactive.Core;

namespace Uno.Extensions.Reactive;

/// <summary>
/// Something that can asynchronously signal that a new item is available.
/// </summary>
/// <typeparam name="T">The type of the item.</typeparam>
public interface ISignal<out T>
{
	/// <summary>
	/// Gets the raw source of this signal.
	/// </summary>
	/// <param name="context">The context for which the source is requested.</param>
	/// <param name="ct">A cancellation to cancel the async enumeration.</param>
	/// <returns>The async enumeration of items produced by this signal.</returns>
	/// <remarks>
	/// This gives access to raw source for implementers and extensibility but it should not be used directly.
	/// Prefer to use extension methods like <see cref="Feed.Messages{T}"/> or <see cref="SourceContext.GetOrCreateSource{T}"/>.
	/// </remarks>
	[EditorBrowsable(EditorBrowsableState.Never)]
	IAsyncEnumerable<T> GetSource(SourceContext context, CancellationToken ct = default);
}
