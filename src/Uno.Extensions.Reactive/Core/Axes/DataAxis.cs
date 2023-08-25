using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Uno.Extensions.Reactive;

/// <summary>
/// The <see cref="MessageAxis"/> of the <see cref="MessageEntry{T}.Data"/>.
/// </summary>
public sealed class DataAxis : MessageAxis
{
	internal static DataAxis Instance { get; } = new();

	private DataAxis()
		: base(MessageAxes.Data)
	{
	}

	/// <summary>
	/// Get the untyped data from the raw axis value.
	/// </summary>
	/// <param name="value">The raw axis value.</param>
	/// <returns>The untyped data.</returns>
	[Pure]
	[EditorBrowsable(EditorBrowsableState.Advanced)]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Option<object> FromMessageValue(MessageAxisValue value)
		=> value is { IsSet: true } and { Value: Option<object> opt } ? opt : Option<object>.Undefined();

	/// <summary>
	/// Get the data from the raw axis value.
	/// </summary>
	/// <param name="value">The raw axis value.</param>
	/// <returns>The data.</returns>
	[Pure]
	[EditorBrowsable(EditorBrowsableState.Advanced)]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public Option<T> FromMessageValue<T>(MessageAxisValue value)
		=> value is { IsSet: true } and { Value: Option<object> opt } ? (Option<T>)opt : Option<T>.Undefined();

	/// <summary>
	/// Encapsulates a untyped data into a raw axis value.
	/// </summary>
	/// <param name="data">The data to encapsulate.</param>
	/// <returns>The raw axis value.</returns>
	[Pure]
	[EditorBrowsable(EditorBrowsableState.Advanced)]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public MessageAxisValue ToMessageValue(Option<object> data)
		=> new(data);

	/// <summary>
	/// Encapsulates a data into a raw axis value.
	/// </summary>
	/// <param name="data">The data to encapsulate.</param>
	/// <returns>The raw axis value.</returns>
	[Pure]
	[EditorBrowsable(EditorBrowsableState.Advanced)]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public MessageAxisValue ToMessageValue<T>(Option<T> data)
		=> new((Option<object>)data);

	/// <inheritdoc />
	[Pure]
	internal override (MessageAxisValue values, IChangeSet? changes) GetLocalValue(MessageAxisValue parent, MessageAxisValue currentLocal, (MessageAxisValue value, IChangeSet? changes) updatedLocal)
		=> updatedLocal;

	/// <inheritdoc />
	[Pure]
	protected internal override MessageAxisValue Aggregate(IEnumerable<MessageAxisValue> values)
		=> throw new NotSupportedException("Data axis values cannot be aggregated.");

	/// <inheritdoc />
	[Pure]
	protected internal override bool AreEquals(MessageAxisValue left, MessageAxisValue right)
		=> OptionEqualityComparer<object>.Default.Equals((Option<object>)left.Value!, (Option<object>)right.Value!);
}
