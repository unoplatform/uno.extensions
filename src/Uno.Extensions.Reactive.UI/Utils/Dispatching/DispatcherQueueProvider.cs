using System;
using System.Linq;
using System.Threading;

namespace Uno.Extensions.Reactive.Dispatching;

internal static class DispatcherQueueProvider
{
	private static readonly ThreadLocal<IDispatcherInternal?> _value = new(CreateForCurrentThread, false);

	public static IDispatcherInternal? GetForCurrentThread()
		=> _value.Value;

	private static IDispatcherInternal? CreateForCurrentThread()
		=> DispatcherQueue.GetForCurrentThread() is { } dispatcher ? new Dispatcher(dispatcher) : null;

	private class Dispatcher : IDispatcherInternal
	{
		private readonly DispatcherQueue _queue;

		public Dispatcher(DispatcherQueue queue)
			=> _queue = queue;

		public bool HasThreadAccess => _queue.HasThreadAccess;

		public void TryEnqueue(Action action)
			=> _queue.TryEnqueue(() => action());
	}
}
