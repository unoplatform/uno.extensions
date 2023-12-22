using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Utils;

namespace Uno.Extensions.Reactive.Operators;

internal sealed class WhereFeed<T> : IFeed<T>
{
	private readonly IFeed<T> _parent;
	private readonly Predicate<Option<T>> _predicate;

	public WhereFeed(IFeed<T> parent, Predicate<T> predicate)
	{
		_parent = parent;
		_predicate = data => data.IsSome(out var value) && predicate(value);
	}

	public WhereFeed(IFeed<T> parent, Predicate<Option<T>> predicate)
	{
		_parent = parent;
		_predicate = predicate;
	}

	/// <inheritdoc />
	public async IAsyncEnumerable<Message<T>> GetSource(SourceContext context, [EnumeratorCancellation] CancellationToken ct = default)
	{
		var localMsg = new MessageManager<T, T>();
		await foreach (var parentMsg in context.GetOrCreateSource(_parent).WithCancellation(ct).ConfigureAwait(false))
		{
			if (localMsg.Update(DoUpdate, parentMsg))
			{
				yield return localMsg.Current;
			}
		}
	}

	private MessageBuilder<T, T> DoUpdate(MessageManager<T, T>.CurrentMessage message, Message<T> parentMsg)
	{
		var updated = message.With(updatedParent: parentMsg);
		if (parentMsg.Changes.Contains(MessageAxis.Data))
		{
			try
			{
				if (_predicate(parentMsg.Current.Data))
				{
					updated
						.Data(parentMsg.Current.Data)
						.Error(null);
				}
				else
				{
					updated
						.Data(Option<T>.None())
						.Error(null);
				}
			}
			catch (Exception error)
			{
				updated
					.Data(Option.Undefined<T>())
					.Error(error);
			}
		}

		return updated;
	}
}
