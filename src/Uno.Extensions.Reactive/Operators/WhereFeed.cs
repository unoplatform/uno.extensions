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
	private readonly Predicate<T?> _predicate;

	public WhereFeed(IFeed<T> parent, Predicate<T?> predicate)
	{
		_parent = parent;
		_predicate = predicate;
	}

	/// <inheritdoc />
	public async IAsyncEnumerable<Message<T>> GetSource(SourceContext context, [EnumeratorCancellation] CancellationToken ct = default)
	{
		var localMsg = new MessageManager<T, T>();
		await foreach (var parentMsg in _parent.GetSource(context, ct).WithCancellation(ct).ConfigureAwait(false))
		{
			if (localMsg.Update(DoUpdate))
			{
				yield return localMsg.Current;
			}

			MessageBuilder<T, T> DoUpdate(MessageManager<T, T>.CurrentMessage message)
			{
				var updated = message.With(updatedParent: parentMsg);
				if (parentMsg.Changes.Contains(MessageAxis.Data)
					&& parentMsg.Current.Data.IsSome(out var value))
				{
					try
					{
						if (_predicate(value))
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
	}
}
