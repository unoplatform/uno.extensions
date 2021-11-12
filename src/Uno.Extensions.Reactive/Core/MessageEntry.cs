using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Uno.Extensions.Reactive;

[DebuggerDisplay("Data: {Data} | Error: {Error} | Transient: {IsTransient}")]
public sealed record MessageEntry<T> : IMessageEntry
{
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

	Option<object> IMessageEntry.Data => (Option<object>)Data;
	Exception? IMessageEntry.Error => Error;
	bool IMessageEntry.IsTransient => IsTransient;
	MessageAxisValue IMessageEntry.this[MessageAxis axis] => this[axis];

	public Option<T> Data => _cachedData ??= this.GetData();
	public Exception? Error => _cachedError ??= this.GetError();
	public bool IsTransient => _cachedProgress ??= this.GetProgress();

	internal MessageAxisValue this[MessageAxis axis] => _values.TryGetValue(axis, out var value) ? value : default;
}
