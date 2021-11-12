using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Uno.Extensions.Reactive;

//internal sealed class CombineFeedHelper<TResult> : IAsyncEnumerable<Message<TResult>>
//{
//	private readonly AsyncEnumerableSubject<Message<TResult>> _subject = new(ReplayMode.EnabledForFirstEnumeratorOnly);
//	private readonly Func<Option<TResult>> _getData;
//	private readonly IMessageEntry[] _parents;

//	private Message<TResult> _message = Message<TResult>.Initial;

//	public CombineFeedHelper(int arity, Func<Option<TResult>> getData)
//	{
//		_getData = getData;
//		_parents = new IMessageEntry[arity];
//	}

//	public void ApplyUpdate<T>(int index, Message<T> parent, ref Option<T> data)
//	{
//		lock (_subject)
//		{
//			_parents[index] = parent.Current;

//			var builder = _message.With();
//			foreach (var changedAxis in parent.Changes)
//			{
//				if (changedAxis == MessageAxis.Data)
//				{
//					data = parent.Current.Data;
//					builder.Data(_getData());
//				}
//				else
//				{
//					var currentValue = builder[changedAxis];
//					var updatedValue = changedAxis.Aggregate(_parents.Select(parent => parent?[changedAxis] ?? default));

//					if (!changedAxis.AreEquals(currentValue, updatedValue))
//					{
//						builder[changedAxis] = updatedValue;
//					}
//				}
//			}

//			_subject.SetNext(_message = builder);
//		}
//	}

//	/// <inheritdoc />
//	public IAsyncEnumerator<Message<TResult>> GetAsyncEnumerator(CancellationToken cancellationToken = default)
//		=> _subject.GetAsyncEnumerator(cancellationToken);
//}

internal sealed class CombineFeedHelper<TResult>
{
	private readonly Func<Option<TResult>> _getData;
	private readonly bool _fastStartForUndefined;
	private readonly bool _fastStartForNone;
	private readonly IReadOnlyDictionary<MessageAxis, MessageAxisValue>?[] _parents;

	private bool _isReady;
	private Message<TResult> _local = Message<TResult>.Initial;

	public CombineFeedHelper(int arity, Func<Option<TResult>> getData, bool fastStartForUndefined = true, bool fastStartForNone = true)
	{
		_parents = new IReadOnlyDictionary<MessageAxis, MessageAxisValue>[arity];
		_getData = getData;
		_fastStartForUndefined = fastStartForUndefined;
		_fastStartForNone = fastStartForNone;
	}

	public Message<TResult>? ApplyUpdate<T>(int index, Message<T> parent, ref Option<T> data)
	{
		_parents[index] = parent.Current.Values;
		data = parent.Current.Data; // We maintain the data up-to-date no matter if ready or not.

		if (_isReady)
		{
			return Build(parent.Changes) is { Changes.Count: > 0 } updated
				? _local = updated
				: null;
		}
		else
		{
			_isReady = InspectParents() switch
			{
				(false, _, _) => true,
				(_, true, _) when _fastStartForUndefined => true,
				(_, _, true) when _fastStartForNone => true,
				_ => false
			};

			if (!_isReady)
			{
				return null;
			}

			var allParentsDefinedAxises = _parents
				.Where(p => p is not null)
				.SelectMany(p => p!.Keys)
				.Distinct();

			return _local = Build(allParentsDefinedAxises);
		}
	}

	private (bool hasNull, bool hasUndefined, bool hasNone) InspectParents()
	{
		bool hasNull = false, hasUndefined = false, hasNone = false;
		foreach (var parent in _parents)
		{
			if (parent is null)
			{
				hasNull = true;
				continue;
			}

			if (!parent.TryGetValue(MessageAxis.Data, out var dataValue))
			{
				hasUndefined = true;
				continue;
			}

			var data =  MessageAxis.Data.FromMessageValue(dataValue);
			if (data.IsUndefined())
			{
				hasUndefined = true;
			}
			else if (data.IsNone())
			{
				hasNone = true;
			}
		}

		return (hasNull, hasUndefined, hasNone);
	}

	private Message<TResult> Build(IEnumerable<MessageAxis> changes)
	{
		var builder = _local.With();
		foreach (var changedAxis in changes)
		{
			if (changedAxis == MessageAxis.Data)
			{
				builder.Data(_getData());
			}
			else
			{
				builder[changedAxis] = changedAxis.Aggregate(_parents.Select(p => p is not null && p.TryGetValue(changedAxis, out var value) ? value : default));
			}
		}

		return builder;
	}
}
