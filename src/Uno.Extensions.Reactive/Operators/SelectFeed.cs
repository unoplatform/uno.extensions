using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Utils;

namespace Uno.Extensions.Reactive.Operators;

internal sealed class SelectFeed<TArg, TResult> : IFeed<TResult>
{
	private readonly IFeed<TArg> _parent;
	private readonly Func<Option<TArg>, Option<TResult>> _projection;

	public SelectFeed(IFeed<TArg> parent, Func<TArg, TResult> projection)
	{
		_parent = parent;
		_projection = projection.SomeOrNoneWhenNotNull();
	}

	public SelectFeed(IFeed<TArg> parent, Func<Option<TArg>, Option<TResult>> projection)
	{
		_parent = parent;
		_projection = projection;
	}

	/// <inheritdoc />
	public async IAsyncEnumerable<Message<TResult>> GetSource(SourceContext context, [EnumeratorCancellation] CancellationToken ct = default)
	{
		var localMsg = new MessageManager<TArg, TResult>();
		await foreach (var parentMsg in context.GetOrCreateSource(_parent).WithCancellation(ct).ConfigureAwait(false))
		{
			if (localMsg.Update(DoUpdate, parentMsg))
			{
				yield return localMsg.Current;
			}
		}
	}

	private MessageBuilder<TArg, TResult> DoUpdate(MessageManager<TArg, TResult>.CurrentMessage current, Message<TArg> parentMsg)
	{
		var updated = current.With(parentMsg!);

		if (parentMsg!.Changes.Contains(MessageAxis.Data))
		{
			try
			{
				updated
					.Data(_projection(parentMsg.Current.Data))
					.Error(null);
			}
			catch (Exception error)
			{
				updated
					.Data(Option.Undefined<TResult>())
					.Error(error);
			}
		}

		return updated;
	}
}
