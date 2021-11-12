using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Reactive.Utils;

namespace Uno.Extensions.Reactive.Impl.Operators;

internal sealed class SelectFeed<TArg, TResult> : IFeed<TResult>
{
	private readonly IFeed<TArg> _parent;
	private readonly Func<TArg?, TResult?> _projection;

	public SelectFeed(IFeed<TArg> parent, Func<TArg?, TResult?> projection)
	{
		_parent = parent;
		_projection = projection;
	}

	/// <inheritdoc />
	public async IAsyncEnumerable<Message<TResult>> GetSource(SourceContext context, [EnumeratorCancellation] CancellationToken ct = default)
	{
		var localMsg = new MessageManager<TArg, TResult>();
		await foreach (var parentMsg in _parent.GetSource(context, ct).WithCancellation(ct).ConfigureAwait(false))
		{
			if (localMsg.Update(DoUpdate))
			{
				yield return localMsg.Current;
			}

			MessageBuilder<TArg, TResult> DoUpdate(MessageManager<TArg, TResult>.ParentedMessage current)
			{
				var updated = current.With(parentMsg!);

				if (parentMsg!.Changes.Contains(MessageAxis.Data)
					&& parentMsg.Current.Data.IsSome(out var value))
				{
					try
					{
						updated
							.Data(Option.Some(_projection(value)))
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
	}
}
