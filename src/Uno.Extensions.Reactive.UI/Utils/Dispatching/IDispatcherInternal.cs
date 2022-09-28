using System;
using System.Linq;

namespace Uno.Extensions.Reactive.Dispatching;

// Unlike the Uno.Extensions.Core.IDispatcher, this represent only a platform agnostic access to the dispatcher.
// It's expected to match the contract of the real dispatcher (i.e. DispatcherQueue) without any kind of decoration.
// It's not intended to be registered in the DI/IoC and the instance is expected to be null when resolved from a background thread, like the real DispatcherQueue.
internal interface IDispatcherInternal
{
	public bool HasThreadAccess { get; }

	public void TryEnqueue(Action action);
}
