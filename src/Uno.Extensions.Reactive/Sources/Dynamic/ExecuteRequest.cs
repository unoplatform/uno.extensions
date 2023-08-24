using System;
using System.Linq;

namespace Uno.Extensions.Reactive.Sources;

/// <summary>
/// A request to re-execute the delegate of a <see cref="DynamicFeed{T}"/>.
/// </summary>
/// <param name="Issuer">The issuer of that request.</param>
/// <param name="Reason">The issuer of that request, for debug purposes.</param>
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
