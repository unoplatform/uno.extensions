using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Utils;

namespace Uno.Extensions.Reactive;

/// <summary>
/// A trigger.
/// </summary>
public sealed class Signal : ISignal, IDisposable
{
	private readonly AsyncEnumerableSubject<Unit> _subject = new(replay: false);

	/// <summary>
	/// Raises a signal.
	/// </summary>
	public void Raise()
		=> _subject.SetNext(Unit.Default);

	/// <inheritdoc />
	IAsyncEnumerable<Unit> ISignal<Unit>.GetSource(SourceContext context, CancellationToken ct)
		=> _subject;

	/// <inheritdoc />
	public void Dispose()
		=> _subject.TryComplete();
}
