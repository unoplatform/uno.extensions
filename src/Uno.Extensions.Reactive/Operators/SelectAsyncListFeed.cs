using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Utils;

namespace Uno.Extensions.Reactive.Operators;

internal class SelectAsyncListFeed<TArg, TResult> : IListFeed<TResult>
{
	private readonly IListFeed<TArg> _parent;
	private readonly Func<TArg, TResult> _syncProjection;
	private readonly AsyncFunc<TArg, TResult, TResult> _asyncProjection;

	public SelectAsyncListFeed(
		IListFeed<TArg> parent,
		Func<TArg, TResult> syncProjection,
		AsyncFunc<TArg, TResult, TResult> asyncProjection)
	{
		_parent = parent;
		_syncProjection = syncProjection;
		_asyncProjection = asyncProjection;
	}

	/// <inheritdoc />
	public IAsyncEnumerable<Message<IImmutableList<TResult>>> GetSource(SourceContext context, CancellationToken ct = default)
	{
		var subject = new AsyncEnumerableSubject<Message<IImmutableList<TResult>>>(AsyncEnumerableReplayMode.EnabledForFirstEnumeratorOnly);
		var message = new MessageManager<IImmutableList<TArg>, IImmutableList<TResult>>(subject.SetNext);
		var helper = new ListAsyncProjectionHelper<TArg, TResult>(_syncProjection, _asyncProjection);
		var isEnumerationCompleted = false;

		BeginEnumeration();

		helper.Updated += OnProjectionUpdated;
		ct.Register(() => helper.Updated -= OnProjectionUpdated);

		return subject;

		async void BeginEnumeration()
		{
			try
			{
				var parentEnumerator = context.GetOrCreateSource(_parent).GetAsyncEnumerator(ct);
				while (await parentEnumerator.MoveNextAsync(ct).ConfigureAwait(false))
				{
					var parentMsg = parentEnumerator.Current;
					if (parentMsg.Changes.Contains(MessageAxis.Data, out var changeSet))
					{
						try
						{
							var data = parentMsg.Current.Data;
							switch (data.Type)
							{
								case OptionType.Undefined:
									helper.Update(ImmutableList<TArg>.Empty);
									message.Update((local, parent) => local.With(parent).Data(Option<IImmutableList<TResult>>.Undefined()).Error(null), parentMsg, ct);
									break;

								case OptionType.None:
									helper.Update(ImmutableList<TArg>.Empty);
									message.Update((local, parent) => local.With(parent).Data(Option<IImmutableList<TResult>>.None()).Error(null), parentMsg, ct);
									break;

								case OptionType.Some:
									helper.Update(data.SomeOrDefault() ?? ImmutableList<TArg>.Empty, changeSet);
									message.Update((local, parent) => local.With(parent).Data(helper.CurrentResult).Error(helper.CurrentError), parentMsg, ct);
									break;

								default:
									throw new NotSupportedException($"Data type '{data.Type}' is not supported.");
							}
						}
						catch (Exception error)
						{
							// Usually this is because the sync projection failed.
							message.Update((local, parent) => local.With(parent).Data(Option<IImmutableList<TResult>>.Undefined()).Error(error), parentMsg, ct);
						}
					}
					else
					{
						message.Update((local, parent) => local.With(parent), parentMsg, ct);
					}
				}

				isEnumerationCompleted = true;
				if (helper.CurrentPending is 0)
				{
					subject.TryComplete();
				}
			}
			catch (Exception error)
			{
				subject.TryFail(error);
			}
		}

		void OnProjectionUpdated(object? _, EventArgs __)
		{
			message.Update(local => local.With().Data(helper.CurrentResult).Error(helper.CurrentError), ct);

			if (isEnumerationCompleted && helper.CurrentPending is 0)
			{
				subject.TryComplete();
			}
		}
	}
}
