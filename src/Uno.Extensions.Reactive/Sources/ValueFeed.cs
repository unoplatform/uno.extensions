using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Uno.Extensions.Reactive.Core;

namespace Uno.Extensions.Reactive.Sources;

/// <summary>
/// A feed that produces a single message with a fixed value and then completes.
/// Unlike <see cref="AsyncFeed{T}"/>, this feed does not support refresh and completes
/// immediately after emitting its value, allowing downstream operators (e.g. UpdateFeed
/// compaction) to detect that no more parent messages will arrive.
/// </summary>
internal sealed class ValueFeed<T> : IFeed<T>
{
	private readonly Option<T> _value;

	public ValueFeed(Option<T> value)
	{
		_value = value;
	}

	/// <inheritdoc />
	public async IAsyncEnumerable<Message<T>> GetSource(SourceContext context, [EnumeratorCancellation] CancellationToken ct = default)
	{
		yield return Message<T>.Initial.With().Data(_value);
	}
}
