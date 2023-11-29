using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Utils;
using static Uno.Extensions.Reactive.Core.FeedHelper;

namespace Uno.Extensions.Reactive.Operators;

internal sealed class SelectAsyncFeed<TArg, TResult> : IFeed<TResult>
{
	private readonly IFeed<TArg> _parent;
	private readonly AsyncFunc<TArg, Option<TResult>> _projection;

	public SelectAsyncFeed(IFeed<TArg> parent, AsyncFunc<TArg, TResult> projection)
	{
		_parent = parent;
		_projection = projection.SomeOrNoneWhenNotNull();
	}

	public SelectAsyncFeed(IFeed<TArg> parent, AsyncFunc<TArg, Option<TResult>> projection)
	{
		_parent = parent;
		_projection = projection;
	}

	/// <inheritdoc />
	public IAsyncEnumerable<Message<TResult>> GetSource(SourceContext context, CancellationToken ct)
	{
		var subject = new AsyncEnumerableSubject<Message<TResult>>(ReplayMode.EnabledForFirstEnumeratorOnly);
		var message = new MessageManager<TArg, TResult>(subject.SetNext);
		var projectionToken = default(CancellationTokenSource);
		var projection = default(Task);

		BeginEnumeration();

		return subject;

		async void BeginEnumeration()
		{
			try
			{
				var parentEnumerator = context.GetOrCreateSource(_parent).GetAsyncEnumerator(ct);
				while (await parentEnumerator.MoveNextAsync(ct).ConfigureAwait(false))
				{
					var parentMsg = parentEnumerator.Current;
					if (parentMsg.Changes.Contains(MessageAxis.Data))
					{
						var data = parentMsg.Current.Data;
						switch (data.Type)
						{
							case OptionType.Undefined:
								projectionToken?.Cancel();
								message.Update((local, parent) => local.With(parent).Data(Option<TResult>.Undefined()).Error(null), parentMsg, ct);
								break;

							case OptionType.None:
								projectionToken?.Cancel();
								message.Update((local, parent) => local.With(parent).Data(Option<TResult>.None()).Error(null), parentMsg, ct);
								break;

							case OptionType.Some:
								var previousProjection = projectionToken;
								projectionToken = CancellationTokenSource.CreateLinkedTokenSource(ct);
								projection = InvokeAsync(message, parentMsg, async ct2 =>  await _projection((TArg)data, ct2), null, context, projectionToken.Token);

								// We prefer to cancel the previous projection only AFTER so we are able to keep existing transient axes (cf. message.BeginTransaction)
								// This will not cause any concurrency issue since a transaction cannot push message updates as soon it's not the current.
								previousProjection?.Cancel(); 
								break;

							default:
								throw new NotSupportedException($"Data type '{data.Type}' is not supported.");
						}
					}
					else
					{
						message.Update((local, parent) => local.With(parent), parentMsg, ct);
					}
				}

				if (projection is not null)
				{
					// Make sure to await the end of the last projection before completing the subject!
					await projection;
				}
				subject.Complete();
			}
			catch (Exception error)
			{
				subject.TryFail(error);
			}
		}
	}
}
