using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Reactive.Utils;
using static Uno.Extensions.Reactive.FeedHelper;

namespace Uno.Extensions.Reactive;

internal class AsyncFeed<T> : IFeed<T>
{
	private readonly ISignal? _refresh;
	private readonly FuncAsync<Option<T>> _dataProvider;

	public AsyncFeed(FuncAsync<Option<T>> dataProvider, ISignal? refresh = null)
	{
		_dataProvider = dataProvider;
		_refresh = refresh;
	}

	/// <inheritdoc />
	public IAsyncEnumerable<Message<T>> GetSource(SourceContext context, CancellationToken ct = default)
	{
		//var current = Message<T>.Initial;

		//// Initial loading of the value
		//await foreach (var message in FeedHelper.InvokeAsync(current, _dataProvider, _dataComparer, context, ct).WithCancellation(ct).ConfigureAwait(false))
		//{
		//	yield return current = message;
		//}

		//// Then subscribe to refresh
		//if (_refresh is not null)
		//{
		//	await foreach (var _ in _refresh.GetSource(context, ct).WithCancellation(ct).ConfigureAwait(false))
		//	{
		//		await foreach (var message in FeedHelper.InvokeAsync(current, _dataProvider, _dataComparer, context, ct).WithCancellation(ct).ConfigureAwait(false))
		//		{
		//			if (message.Changes != default)
		//			{
		//				yield return current = message;
		//			}
		//		}
		//	}
		//}

		async IAsyncEnumerable<Unit> Triggers([EnumeratorCancellation] CancellationToken token = default)
		{
			// Initial loading of teh value
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
				async (_, ct) => await InvokeAsync(message, _dataProvider, context, ct),
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
