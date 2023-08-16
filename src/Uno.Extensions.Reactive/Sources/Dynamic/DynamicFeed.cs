using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Uno.Extensions.Reactive.Core;

namespace Uno.Extensions.Reactive.Sources;

internal sealed class DynamicFeed<T> : IFeed<T>
{
	private readonly AsyncFunc<Option<T>> _dataProvider;

	public DynamicFeed(AsyncFunc<T?> dataProvider)
	{
		_dataProvider = async ct => Option.SomeOrNone(await dataProvider(ct));
	}

	public DynamicFeed(AsyncFunc<Option<T>> dataProvider)
	{
		_dataProvider = dataProvider;
	}

	/// <inheritdoc />
	public async IAsyncEnumerable<Message<T>> GetSource(SourceContext context, [EnumeratorCancellation] CancellationToken ct = default)
	{
		await using var session = new FeedSession<T>(this, context, _dataProvider, ct);
		while (await session.MoveNextAsync())
		{
			yield return session.Current;
		}
	}
}
