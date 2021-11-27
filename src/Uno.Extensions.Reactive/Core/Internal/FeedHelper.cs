using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Extensions.Reactive.Core;

internal static class FeedHelper
{
	/// <summary>
	/// Invokes an async method in reaction to a message from a parent feed
	/// </summary>
	/// <typeparam name="TParent">Value type of the parent feed</typeparam>
	/// <typeparam name="TResult">Value type of the resulting feed</typeparam>
	/// <param name="msgManager">The message manager used to produce new values messages.</param>
	/// <param name="dataProvider">The async method to invoke.</param>
	/// <param name="context">The context to use to invoke the async method.</param>
	/// <param name="ct">The enumerator cancellation.</param>
	/// <returns>
	/// An async enumerable that produces message that has to be forwarded (and stored as new current) by the resulting feed
	/// to track the progress the of the async method invocation.
	/// In best cases this will contains only one message (e.g. <paramref name="dataProvider"/> runs sync),
	/// and currently it will produces a maximum of 2 messages.
	/// </returns>
	public static async Task InvokeAsync<TParent, TResult>(
		MessageManager<TParent, TResult> msgManager, 
		AsyncFunc<Option<TResult>> dataProvider,
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
