using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Uno.Extensions.Reactive.Utils;

internal class ConditionalDeferredAsyncEnumerable<T> : IAsyncEnumerable<T>
{
	private readonly IAsyncEnumerable<T> _inner;
	private readonly Func<bool> _deferringCondition;

	public ConditionalDeferredAsyncEnumerable(IAsyncEnumerable<T> inner, Func<bool> deferringCondition)
	{
		_inner = inner;
		_deferringCondition = deferringCondition;
	}

	/// <inheritdoc />
	public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken ct = default)
		=> new ConditionalDeferredAsyncEnumerator<T>(_inner.GetAsyncEnumerator(ct), _deferringCondition);
}
