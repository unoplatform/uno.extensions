using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Uno.Extensions.Reactive.Sources;

namespace Uno.Extensions.Reactive;

/// <summary>
/// The entry of a <see cref="Message{T}"/> which aggregates values of all the set <see cref="MessageAxis"/>.
/// </summary>
/// <typeparam name="T">The type of the value of the entry.</typeparam>
[DebuggerDisplay("Data: {Data} | Error: {Error} | Transient: {IsTransient}")]
public sealed record MessageEntry<T> : IMessageEntry, IMessageEntry<T>
{
	/// <summary>
	/// The initial empty entry.
	/// </summary>
	public static MessageEntry<T> Empty { get; } = new(new Dictionary<MessageAxis, MessageAxisValue> { { MessageAxis.Data, new(Option<object>.Undefined()) } });

	private readonly IReadOnlyDictionary<MessageAxis, MessageAxisValue> _values;

	private Option<T>? _cachedData;
	private Exception? _cachedError;
	private bool? _cachedProgress;

	internal MessageEntry(IReadOnlyDictionary<MessageAxis, MessageAxisValue> values)
	{
		Debug.Assert(values.ContainsKey(MessageAxis.Data), "Data axis must always be set. You can use Option.Undefined if you don't have any data to provide.");

		_values = values;
	}

	internal IReadOnlyDictionary<MessageAxis, MessageAxisValue> Values => _values;

	/// <summary>
	/// The data of this entry.
	/// </summary>
	public Option<T> Data
	{
		get
		{
			if (_cachedData is null)
			{
				_cachedData = this.GetData();
			}
			else
			{
				FeedDependency.NotifyTouched(this, MessageAxis.Data);
			}

			return _cachedData.Value;
		}
	}
	Option<object> IMessageEntry.Data => (Option<object>)Data;

	/// <summary>
	/// The error associated to that entry, if any.
	/// </summary>
	public Exception? Error
	{
		get
		{
			if (_cachedError is null)
			{
				_cachedError = this.GetError();
			}
			else
			{
				FeedDependency.NotifyTouched(this, MessageAxis.Error);
			}

			return _cachedError;
		}
	}
	Exception? IMessageEntry.Error => Error;

	/// <summary>
	/// Indicates if this entry should be considered as transient or not.
	/// </summary>
	public bool IsTransient
	{
		get
		{
			if (_cachedProgress is null)
			{
				_cachedProgress = this.GetProgress();
			}
			else
			{
				FeedDependency.NotifyTouched(this, MessageAxis.Error);
			}

			return _cachedProgress.Value;
		}
	}
	bool IMessageEntry.IsTransient => IsTransient;

	internal MessageAxisValue this[MessageAxis axis]
	{
		get
		{
			FeedDependency.NotifyTouched(this, axis);

			return _values.TryGetValue(axis, out var value)
				? value
				: default;
		}
	}
	MessageAxisValue IMessageEntry.this[MessageAxis axis] => this[axis];

	/// <inheritdoc />
	IEnumerator IEnumerable.GetEnumerator()
		=> ((IEnumerable)_values).GetEnumerator();

	/// <inheritdoc />
	IEnumerator<KeyValuePair<MessageAxis, MessageAxisValue>> IEnumerable<KeyValuePair<MessageAxis, MessageAxisValue>>.GetEnumerator()
		=> _values.GetEnumerator();
}
