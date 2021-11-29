using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Uno.Extensions.Reactive;

/// <summary>
/// The <see cref="MessageAxis"/> of the <see cref="MessageEntry{T}.IsTransient"/>.
/// </summary>
public sealed class ProgressAxis : MessageAxis
{
	internal static ProgressAxis Instance { get; } = new();

	private ProgressAxis()
		: base(MessageAxes.Progress)
	{
	}

	/// <summary>
	/// Get the progress from the raw axis value.
	/// </summary>
	/// <param name="value">The raw axis value.</param>
	/// <returns>The progress.</returns>
	[Pure]
	[EditorBrowsable(EditorBrowsableState.Advanced)]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool FromMessageValue(MessageAxisValue value)
		=> value is { IsSet: true } and { Value: bool and true };

	/// <summary>
	/// Encapsulates a progress into a raw axis value.
	/// </summary>
	/// <param name="progress">The progress to encapsulate.</param>
	/// <returns>The raw axis value.</returns>
	[Pure]
	[EditorBrowsable(EditorBrowsableState.Advanced)]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public MessageAxisValue ToMessageValue(bool progress)
		=> progress ? new(true) : default;

	/// <inheritdoc />
	[Pure]
	protected internal override MessageAxisValue Aggregate(IEnumerable<MessageAxisValue> progresses)
		=> progresses.Any(FromMessageValue) ? new(true) : default;

	/// <inheritdoc />
	[Pure]
	protected internal override bool AreEquals(MessageAxisValue left, MessageAxisValue right)
		=> left.IsSet == right.IsSet && left.Value == right.Value;
}
