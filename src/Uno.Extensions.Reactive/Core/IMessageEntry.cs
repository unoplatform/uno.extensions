using System;
using System.ComponentModel;
using System.Linq;

namespace Uno.Extensions.Reactive;

/// <summary>
/// Untyped interface of <see cref="MessageEntry{T}"/>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Advanced)] // Used only for bindings
public interface IMessageEntry : IEnumerable<KeyValuePair<MessageAxis, MessageAxisValue>>
{
	/// <summary>
	/// The data of this entry.
	/// </summary>
	public Option<object> Data { get; }

	/// <summary>
	/// The error associated to that entry, if any.
	/// </summary>
	public Exception? Error { get; }

	/// <summary>
	/// Indicates if this entry should be considered as transient or not.
	/// </summary>
	public bool IsTransient { get; }

	/// <summary>
	/// Gets the raw value of a given axis.
	/// </summary>
	/// <param name="axis">The axis to get.</param>
	/// <returns>The raw value of the axis.</returns>
	/// <remarks>
	/// This gives access to raw <see cref="MessageAxisValue"/> for extensibility but it should not be used directly.
	/// Prefer to use dedicated extensions methods to access to values.
	/// </remarks>
	/// <remarks>
	/// Getting the value of an axis that is not set will not throw,
	/// it will instead return <see cref="MessageAxisValue.Unset"/>.
	/// </remarks>
	[EditorBrowsable(EditorBrowsableState.Never)] // Use dedicated extensions methods
	MessageAxisValue this[MessageAxis axis] { get; }
}

internal interface IMessageEntry<T> : IMessageEntry
{
	/// <summary>
	/// The data of this entry.
	/// </summary>
	public new Option<T> Data { get; }

	// TODO: Provide default interface implementation once updated to .net 7!
	//Option<object> IMessageEntry.Data => Data;
}
