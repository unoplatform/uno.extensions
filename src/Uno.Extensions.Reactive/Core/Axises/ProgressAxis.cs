using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Uno.Extensions.Reactive;

public sealed class ProgressAxis : MessageAxis
{
	internal static ProgressAxis Instance { get; } = new();

	private ProgressAxis()
		: base(MessageAxises.Progress)
	{
	}

	[Pure]
	[EditorBrowsable(EditorBrowsableState.Advanced)]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public bool FromMessageValue(MessageAxisValue progress)
		=> progress is { IsSet: true } and { Value: bool and true };

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
