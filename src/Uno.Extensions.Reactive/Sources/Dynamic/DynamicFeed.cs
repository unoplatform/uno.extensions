using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Uno.Extensions.Reactive.Config;
using Uno.Extensions.Reactive.Core;

namespace Uno.Extensions.Reactive.Sources;

internal sealed class DynamicFeed<T> : IFeed<T>
{
	private readonly AsyncFunc<Option<T>> _dataProvider;

	public DynamicFeed(AsyncFunc<T?> dataProvider)
	{
		_dataProvider = async ct => Option.SomeOrNone(await dataProvider(ct).ConfigureAwait(false));
	}

	public DynamicFeed(AsyncFunc<Option<T>> dataProvider)
	{
		_dataProvider = dataProvider;
	}

	/// <inheritdoc />
	public async IAsyncEnumerable<Message<T>> GetSource(SourceContext context, [EnumeratorCancellation] CancellationToken ct = default)
	{
#pragma warning disable CA2007 // Consider calling ConfigureAwait on the awaited task, false positive: it's for the DisposeAsync which cannot be configured here
		await using var session = new FeedSession<T>(this, context, _dataProvider, ct);
#pragma warning restore CA2007

		while (await session.MoveNextAsync().ConfigureAwait(false))
		{
			yield return session.Current;
		}
	}
}
