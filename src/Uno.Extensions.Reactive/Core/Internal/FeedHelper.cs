using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Reactive.Utils;

namespace Uno.Extensions.Reactive;

internal static class FeedHelper
{
	/// <summary>
	/// Invokes an async method in reaction to a message from a parent feed
	/// </summary>
	/// <typeparam name="TParent">Value type of the parent feed</typeparam>
	/// <typeparam name="TResult">Value type of the resulting feed</typeparam>
	/// <param name="parent">The message from the parent that is driving the <see cref="valueProvider"/> to be invoked.</param>
	/// <param name="local">The last message published by the resulting feed.</param>
	/// <param name="dataProvider">The async method to invoke.</param>
	/// <param name="context">The context to use to invoke the async method.</param>
	/// <param name="ct">The enumerator cancellation.</param>
	/// <returns>
	/// An async enumerable that produces message that has to be forwarded (and stored as new current) by the resulting feed
	/// to track the progress the of the async method invocation.
	/// In best cases this will contains only one message (e.g. <paramref name="dataProvider"/> runs sync),
	/// and currently it will produces a maximum of 2 messages.
	/// </returns>
	public static Task InvokeAsync<TParent, TResult>(
		MessageManager<TParent, TResult> message, 
		FuncAsync<Option<TResult>> dataProvider,
		SourceContext context, 
		CancellationToken ct)
		=> InvokeCore(message, dataProvider, context, ct);


	///// <summary>
	///// Invokes an async method.
	///// </summary>
	///// <typeparam name="TResult">Value type of the resulting feed</typeparam>
	///// <param name="current">The last message published by the resulting feed.</param>
	///// <param name="dataProvider">The async method to invoke.</param>
	///// <param name="context">The context to use to invoke the async method.</param>
	///// <param name="ct">The enumerator cancellation.</param>
	///// <returns>
	///// An async enumerable that produces message that has to be forwarded (and stored as new current) by the resulting feed
	///// to track the progress the of the async method invocation.
	///// In best cases this will contains only one message (e.g. <paramref name="dataProvider"/> runs sync),
	///// and currently it will produces a maximum of 2 messages.
	///// </returns>
	//public static IAsyncEnumerable<Message<TResult>> InvokeAsync<TResult>(
	//	Message<TResult> current, 
	//	FuncAsync<Option<TResult>> dataProvider, 
	//	IEqualityComparer<Option<TResult>> dataComparer,
	//	SourceContext context, 
	//	CancellationToken ct)
	//	=> InvokeCore<object, TResult>(default, current, dataProvider, dataComparer, context, ct);

	//private static async IAsyncEnumerable<Message<TResult>> InvokeCore<TParent, TResult>(
	//	Message<TParent>? parent,
	//	Message<TResult> local, 
	//	FuncAsync<Option<TResult>> dataProvider, 
	//	IEqualityComparer<Option<TResult>> dataComparer,
	//	SourceContext context, 
	//	[EnumeratorCancellation] CancellationToken ct)
	//{
	//	using var _ = context.AsCurrent();

	//	var builder = parent is null ? local.With() : local.With(parent);

	//	ValueTask<Option<TResult>> dataTask = default;
	//	Exception? error = default;
	//	try
	//	{
	//		dataTask = dataProvider(ct);
	//	}
	//	catch (Exception e)
	//	{
	//		error = e;
	//	}

	//	if (error is not null)
	//	{
	//		yield return builder.Error(AggregateErrors(parent?.Current.Error, error));
	//		yield break;
	//	}

	//	// We try to delay a bit the transient state if possible,
	//	// but is the parent is transient or we are already in transient state it's useless to try that.
	//	//var isAlreadyTransient = ((IMessageEntry)message).IsTransient; // This will also be true if parent used in the message.with() is transient.
	//	if (!local.Current.IsTransient)
	//	{
	//		// If possible do not go in transient state
	//		for (var i = 0; !dataTask.IsCompleted && !ct.IsCancellationRequested && i < 5; i++)
	//		{
	//			await Task.Yield();
	//		}

	//		if (ct.IsCancellationRequested)
	//		{
	//			yield break;
	//		}

	//		// The 'valueProvider' is not completed yet and we were not already in transient state,
	//		// so we need to flag the current value as transient.
	//		if (!dataTask.IsCompleted)
	//		{
	//			Message<TResult> updated = builder.IsTransient(true);
	//			yield return updated;
	//			builder = updated.With(); // Make sure that next message is built from the last published one
	//		}
	//	}

	//	try
	//	{
	//		var data = await dataTask;
	//		if (!dataComparer.Equals(builder.CurrentData, data))
	//		{
	//			builder.Data(data);
	//		}

	//		// If the parent has error but the last local published value does have one, we make sure to clear it.
	//		if (parent?.Current.Error is null && builder.CurrentError is not null)
	//		{
	//			builder.Error(null);
	//		}
	//	}
	//	catch (Exception localError)
	//	{
	//		builder.Error(AggregateErrors(parent?.Current.Error, localError));
	//	}

	//	// If the parent is not transient but the last local published value was, we make sure to clear the transient flag.
	//	if (!(parent?.Current.IsTransient ?? false) && builder.CurrentIsTransient)
	//	{
	//		builder.IsTransient(false);
	//	}

	//	yield return builder;
	//}

	private static async Task InvokeCore<TParent, TResult>(
		MessageManager<TParent, TResult> msgManager,
		FuncAsync<Option<TResult>> dataProvider,
		SourceContext context,
		CancellationToken ct)
	{
		// Note: We DO NOT register the 'message' update transaction into ct.Register, so in case of a "Last wins" usage of this,
		//		 we allow the next updater to really preserver the pending progress axis.
		using var message = msgManager.BeginUpdate(ct, preservePendingAxises: MessageAxis.Progress);
		using var _ = context.AsCurrent();

		ValueTask<Option<TResult>> dataTask = default;
		Exception? error = default;
		try
		{
			dataTask = dataProvider(ct);
		}
		catch (OperationCanceledException) when (ct.IsCancellationRequested)
		{
			return;
		}
		catch (Exception e)
		{
			error = e;
		}

		if (error is not null)
		{
			message.Commit(m => m.With().Error(error));
			return;
		}

		// If we are not yet and the 'dataTask' is really async, we need to send a new message flagged as transient
		// Note: This check is not "atomic", but it's valid as it only enables a fast path.
		if (!message.Local.Current.IsTransient)
		{
			// As lot of async methods are actually not really async but only re-scheduled,
			// we try to avoid the transient state by delaying a bit the message.
			for (var i = 0; !dataTask.IsCompleted && !ct.IsCancellationRequested && i < 5; i++)
			{
				await Task.Yield();
			}

			if (ct.IsCancellationRequested)
			{
				return;
			}

			// The 'valueProvider' is not completed yet, so we need to flag the current value as transient.
			// Note: If the 'message' has been put in 
			if (!dataTask.IsCompleted)
			{
				message.TransientSet(MessageAxis.Progress, true);
			}
		}

		Option<TResult> data = default;
		try
		{
			data = await dataTask;
		}
		catch (OperationCanceledException) when (ct.IsCancellationRequested)
		{
			return;
		}
		catch (Exception e)
		{
			error = e;
		}

		message.Commit(msg =>
		{
			var builder = msg.With();
			if (error is null)
			{
				// Clear the local error if any.
				// Note: Thanks to the MessageManager, this will NOT erase the parent's error!
				builder.Data(data).Error(null);
			}
			else
			{
				builder.Error(error);
			}

			return builder;
		});
	}

	public static Exception? AggregateErrors(Exception? error1, Exception? error2)
	{
		if (error1 is null)
		{
			return error2;
		}

		if (error2 is null)
		{
			return error1;
		}

		List<Exception> errors = new();
		if (error1 is AggregateException aggregate1)
		{
			errors.AddRange(aggregate1.InnerExceptions);
		}
		else
		{
			errors.Add(error1);
		}
		if (error2 is AggregateException aggregate2)
		{
			errors.AddRange(aggregate2.InnerExceptions);
		}
		else
		{
			errors.Add(error1);
		}

		return new AggregateException(errors);
	}

	public static Exception AggregateErrors(IReadOnlyCollection<Exception> errors)
	{
		var flattened = errors
			.SelectMany(error => error switch
			{
				null => Enumerable.Empty<Exception>(),
				AggregateException aggregate => aggregate.InnerExceptions,
				_ => new[] { error }
			});

		return new AggregateException(flattened);
	}

	public static MessageAxisValue AggregateErrors(MessageAxisValue left, MessageAxisValue right)
	{
		if (AggregateErrors((Exception?)left.Value, (Exception?)right.Value) is { } error)
		{
			return new(error);
		}
		else
		{
			return MessageAxisValue.Unset;
		}
	}
}
