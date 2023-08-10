using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
<<<<<<< HEAD
=======
using System.Text;
using Uno.Extensions.Reactive.Sources;
>>>>>>> 3d7cd0bd (fix(reg): Fix possible stack-overflow when loging is enabled)

namespace Uno.Extensions.Reactive;

/// <summary>
/// The entry of a <see cref="Message{T}"/> which aggregates values of all the set <see cref="MessageAxis"/>.
/// </summary>
/// <typeparam name="T">The type of the value of the entry.</typeparam>
[DebuggerDisplay("Data: {Data} | Error: {Error} | Transient: {IsTransient}")]
public sealed record MessageEntry<T> : IMessageEntry
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
	public Option<T> Data => _cachedData ??= this.GetData();
	Option<object> IMessageEntry.Data => (Option<object>)Data;

	/// <summary>
	/// The error associated to that entry, if any.
	/// </summary>
	public Exception? Error => _cachedError ??= this.GetError();
	Exception? IMessageEntry.Error => Error;

	/// <summary>
	/// Indicates if this entry should be considered as transient or not.
	/// </summary>
	public bool IsTransient => _cachedProgress ??= this.GetProgress();
	bool IMessageEntry.IsTransient => IsTransient;

	internal MessageAxisValue this[MessageAxis axis] => _values.TryGetValue(axis, out var value) ? value : default;
	MessageAxisValue IMessageEntry.this[MessageAxis axis] => this[axis];
<<<<<<< HEAD
=======

	/// <inheritdoc />
	IEnumerator IEnumerable.GetEnumerator()
		=> ((IEnumerable)_values).GetEnumerator();

	/// <inheritdoc />
	IEnumerator<KeyValuePair<MessageAxis, MessageAxisValue>> IEnumerable<KeyValuePair<MessageAxis, MessageAxisValue>>.GetEnumerator()
		=> _values.GetEnumerator();

	/// <inheritdoc />
	public override string ToString()
	{
		// ToString, used for debug, MUST NOT NotifyTouched

		var data = _cachedData;
		if (data is null && _values.TryGetValue(MessageAxis.Data, out var dataValue))
		{
			data = MessageAxis.Data.FromMessageValue<T>(dataValue);
		}

		var error = _cachedError;
		if (error is null && _values.TryGetValue(MessageAxis.Error, out var errorValue))
		{
			error = MessageAxis.Error.FromMessageValue(errorValue);
		}

		var progress = _cachedProgress;
		if (progress is null && _values.TryGetValue(MessageAxis.Progress, out var progressValue))
		{
			progress = MessageAxis.Progress.FromMessageValue(progressValue);
		}

		var str = new StringBuilder($"Data={data} | Error={error?.GetType().Name ?? "--null--"} | IsTransient={progress ?? false}");
		foreach (var value in _values)
		{
			if (value.Key == MessageAxis.Data
				|| value.Key == MessageAxis.Error
				|| value.Key == MessageAxis.Progress)
			{
				continue;
			}

			str.Append($" | {value.Key.Identifier}={value.Value}");
		}

		return str.ToString();
	}
>>>>>>> 3d7cd0bd (fix(reg): Fix possible stack-overflow when loging is enabled)
}
