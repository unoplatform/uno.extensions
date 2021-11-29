using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Uno.Extensions.Reactive;

/// <summary>
/// Untyped interface of <see cref="Message{T}"/>
/// </summary>
[EditorBrowsable(EditorBrowsableState.Advanced)] // Used only for bindings
public interface IMessage
{
	/// <summary>
	/// The previous entry.
	/// </summary>
	IMessageEntry Previous { get; }

	/// <summary>
	/// The current entry.
	/// </summary>
	IMessageEntry Current { get; }

	/// <summary>
	/// The axes that has been modified in <see cref="Current"/> compared to <see cref="Previous"/>.
	/// </summary>
	IReadOnlyCollection<MessageAxis> Changes { get; }
}
