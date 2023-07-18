using System;
using System.Linq;

namespace Uno.Extensions.Reactive.Sources;

internal record ExecuteRequest(object Issuer, string Reason)
{
	/// <summary>
	/// Defined the axis used to flag the message as async (i.e. transient).
	/// This can be customized by sub-classer, in conjunction with the <see cref="AsyncValue"/>.
	/// </summary>
	internal virtual MessageAxis AsyncAxis => MessageAxis.Progress;

	/// <summary>
	/// Flags the message as async (i.e. transient).
	/// This can be customized by sub-classer, in conjunction with the <see cref="AsyncAxis"/>.
	/// </summary>
	internal virtual MessageAxisValue AsyncValue => MessageAxis.Progress.ToMessageValue(true);

	internal virtual bool IsAsync(IMessageEntry entry)
		=> entry.IsTransient;
}
