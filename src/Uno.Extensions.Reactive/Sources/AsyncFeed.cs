using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Utils;
using static Uno.Extensions.Reactive.Core.FeedHelper;

namespace Uno.Extensions.Reactive.Sources;

internal sealed class AsyncFeed<T> : IFeed<T>
{
	private readonly ISignal? _refresh;
	private readonly AsyncFunc<Option<T>> _dataProvider;

	public AsyncFeed(AsyncFunc<T> dataProvider, ISignal? refresh = null)
	{
		_dataProvider = async ct => Option.SomeOrNone(await dataProvider(ct));
		_refresh = refresh;
	}

	public AsyncFeed(AsyncFunc<Option<T>> dataProvider, ISignal? refresh = null)
	{
		_dataProvider = dataProvider;
		_refresh = refresh;
	}

	/// <inheritdoc />
	public IAsyncEnumerable<Message<T>> GetSource(SourceContext context, CancellationToken ct = default)
	{
		async IAsyncEnumerable<Unit> Triggers([EnumeratorCancellation] CancellationToken token = default)
		{
			// Initial loading of the value
			yield return Unit.Default;

			// Then subscribe to refresh
			if (_refresh is not null)
			{
				await foreach (var _ in _refresh.GetSource(context, token).WithCancellation(token).ConfigureAwait(false))
				{
					yield return Unit.Default;
				}
			}
		}

		var subject = new AsyncEnumerableSubject<Message<T>>(ReplayMode.EnabledForFirstEnumeratorOnly);
		var message = new MessageManager<Unit, T>(subject.SetNext);

		Triggers(ct)
			.ForEachAwaitWithCancellationAsync(
				async (_, ct) => await InvokeAsync(message, null, _dataProvider, context, ct),
				ConcurrencyMode.IgnoreNew, // If _refresh is triggered multiple times, we want to refresh only once.
				continueOnError: true,
				ct)
			.ContinueWith(
				t =>
				{
					if (t.IsFaulted)
					{
						subject.TryFail(t.Exception!);
					}
					else
					{
						subject.Complete();
					}
				},
				TaskContinuationOptions.AttachedToParent | TaskContinuationOptions.ExecuteSynchronously);

		return subject;
	}
}
