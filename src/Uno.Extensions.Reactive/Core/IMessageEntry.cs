using System;
using System.ComponentModel;
using System.Linq;

namespace Uno.Extensions.Reactive;

public interface IMessageEntry
{
	public Option<object> Data { get; }

	public Exception? Error { get; }

	public bool IsTransient { get; }

	[EditorBrowsable(EditorBrowsableState.Never)]
	MessageAxisValue this[MessageAxis axis] { get; }
}
