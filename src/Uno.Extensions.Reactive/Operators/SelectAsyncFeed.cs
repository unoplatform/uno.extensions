using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Uno.Extensions.Reactive.Utils;
using static Uno.Extensions.Reactive.FeedHelper;
using Exception = System.Exception;

namespace Uno.Extensions.Reactive.Impl.Operators;

internal sealed class SelectAsyncFeed<TArg, TResult> : IFeed<TResult>
{
	private readonly IFeed<TArg> _parent;
	private readonly FuncAsync<TArg?, TResult?> _projection;

	public SelectAsyncFeed(IFeed<TArg> parent, FuncAsync<TArg?, TResult?> projection)
	{
		_parent = parent;
		_projection = projection;
	}

	///// <inheritdoc />
	//public async IAsyncEnumerable<Message<TResult>> GetSource(SourceContext context, [EnumeratorCancellation] CancellationToken ct = default)
	//{
	//	var current = Message<TResult>.Initial;
	//	var token = default(CancellationTokenSource);

	//	await foreach (var parentMsg in _parent.GetSource(context, ct).WithCancellation(ct).ConfigureAwait(false))
	//	{
	//		if (parentMsg.Changes.HasFlag(MessageAxis.Data))
	//		{
	//			// For SelectAsync we are in "last wins" mode, i.e. we cancel the previous request to get the updated value
	//			token?.Cancel();

	//			var data = parentMsg.Current.Data;
	//			switch (data.Type)
	//			{
	//				case OptionType.Undefined:
	//					yield return current = current.With(parentMsg).Data(Option<TResult>.Undefined());
	//					break;

	//				case OptionType.None:
	//					yield return current = current.With(parentMsg).Data(Option<TResult>.None());
	//					break;

	//				case OptionType.Some:
	//					token = CancellationTokenSource.CreateLinkedTokenSource(ct);
	//					await foreach (var invokeMsg in FeedHelper
	//						.InvokeAsync(parentMsg, current, async ct2 => await _projection((TArg?)data, ct2), _dataComparer, context, token.Token)
	//						.WithCancellation(token.Token)
	//						.ConfigureAwait(false))
	//					{
	//						yield return current = invokeMsg;
	//					}

	//					break;
	//			}
	//		}
	//		else
	//		{
	//			yield return current = current.With(parentMsg);
	//		}
	//	}
	//}

	//private class AsyncEnumerator<T> : IAsyncEnumerator<T>
	//{
			

	//	/// <inheritdoc />
	//	public ValueTask DisposeAsync()
	//		=> throw new NotImplementedException();

	//	/// <inheritdoc />
	//	public ValueTask<bool> MoveNextAsync()
	//	{

	//	}

	//	/// <inheritdoc />
	//	public T Current { get; }

	//	public void SetCurrent(T current)
	//	{

	//	}

	//	public void Complete()
	//	{

	//	}
	//}

	/// <inheritdoc />
	public IAsyncEnumerable<Message<TResult>> GetSource(SourceContext context, CancellationToken ct)
	{
		var subject = new AsyncEnumerableSubject<Message<TResult>>(ReplayMode.EnabledForFirstEnumeratorOnly);
		var message = new MessageManager<TArg, TResult>(subject.SetNext);
		var projectionToken = default(CancellationTokenSource);

		BeginEnumeration();

		return subject;

		async void BeginEnumeration()
		{
			try
			{
				var parentEnumerator = _parent.GetSource(context, ct).GetAsyncEnumerator(ct);
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
								message.Update(msg => msg.With(parentMsg).Data(Option<TResult>.Undefined()).Error(null), ct);
								break;

							case OptionType.None:
								projectionToken?.Cancel();
								message.Update(msg => msg.With(parentMsg).Data(Option<TResult>.None()).Error(null), ct);
								break;

							case OptionType.Some:
								var previousProjection = projectionToken;
								projectionToken = CancellationTokenSource.CreateLinkedTokenSource(ct);
								_ = InvokeAsync(message, async ct2 => await _projection((TArg?)data, ct2), context, projectionToken.Token);

								// We prefer to cancel the previous projection only AFTER so we are able to keep existing transient axises (cf. message.BeginTransaction)
								// This will not cause any concurrency issue since a transaction cannot push message updates as soon it's not the current.
								previousProjection?.Cancel(); 
								break;

							default:
								throw new NotSupportedException($"Data type '{data.Type}' is not supported.");
						}
					}
					else
					{
						message.Update(msg => msg.With(parentMsg), ct);
					}
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
