using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Uno.Extensions.Reactive;

public sealed class DataAxis : MessageAxis
{
	internal static DataAxis Instance { get; } = new();

	private DataAxis()
		: base(MessageAxises.Data)
	{
	}

	[Pure]
	[EditorBrowsable(EditorBrowsableState.Advanced)]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Option<object> FromMessageValue(MessageAxisValue value)
		=> value is { IsSet: true } and { Value: Option<object> opt } ? opt : Option<object>.Undefined();

	[Pure]
	[EditorBrowsable(EditorBrowsableState.Advanced)]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Option<T> FromMessageValue<T>(MessageAxisValue value)
		=> value is { IsSet: true } and { Value: Option<object> opt } ? (Option<T>)opt : Option<T>.Undefined();

	[Pure]
	[EditorBrowsable(EditorBrowsableState.Advanced)]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public MessageAxisValue ToMessageValue(Option<object> value)
		=> new(value);

	[Pure]
	[EditorBrowsable(EditorBrowsableState.Advanced)]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public MessageAxisValue ToMessageValue<T>(Option<T> value)
		=> new((Option<object>)value);

	/// <inheritdoc />
	[Pure]
	internal override MessageAxisValue GetLocalValue(MessageAxisValue parent, MessageAxisValue local)
		=> local;

	/// <inheritdoc />
	[Pure]
	protected internal override MessageAxisValue Aggregate(IEnumerable<MessageAxisValue> values)
		=> throw new NotSupportedException("Data axis values cannot be aggregated.");

	/// <inheritdoc />
	[Pure]
	protected internal override bool AreEquals(MessageAxisValue left, MessageAxisValue right)
		=> OptionEqualityComparer<object>.RefEquals.Equals((Option<object>)left.Value!, (Option<object>)right.Value!);
}
