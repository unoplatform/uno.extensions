using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Extensions.Reactive.Dispatching;

/// <summary>
/// Provider of <see cref="IDispatcher"/>.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class DispatcherQueueProvider
{
	private static readonly ThreadLocal<IDispatcher?> _value = new(CreateForCurrentThread, false);

	/// <summary>
	/// Gets a dispatcher queue instance that will execute tasks serially on the current thread, or null if no such queue exists.
	/// </summary>
	/// <returns>The dispatcher associated to the current thread if the thread is a UI thread.</returns>
	public static IDispatcher? GetForCurrentThread()
	{
		try
		{
			return _value.Value;
		}
		catch (ObjectDisposedException)
		{
			// _value is a static that is never disposed explicitly: it is reclaimed by its OWN finalizer once the
			// AssemblyLoadContext holding it is collected. Finalization order is unspecified, so another finalizer
			// running in the same pass can reach this after the ThreadLocal is gone. Throwing here would escape the
			// finalizer thread and terminate the process; null is already a documented result of this method, and
			// means the same thing in practice — there is no dispatcher for this thread any more.
			return null;
		}
	}

	private static IDispatcher? CreateForCurrentThread()
		=> DispatcherQueue.GetForCurrentThread() is { } dispatcher ? new Dispatcher(dispatcher) : null;

	private class Dispatcher : IDispatcher
	{
		private readonly DispatcherQueue _queue;

		public Dispatcher(DispatcherQueue queue)
			=> _queue = queue;

		/// <inheritdoc />
		public bool HasThreadAccess => _queue.HasThreadAccess;

		/// <inheritdoc />
		public bool TryEnqueue(Action action)
			=> _queue.TryEnqueue(() => action());

		/// <inheritdoc />
		public async ValueTask<TResult> ExecuteAsync<TResult>(AsyncFunc<TResult> action, CancellationToken ct)
		{
			var tcs = new TaskCompletionSource<TResult>();
			using var ctReg = ct.CanBeCanceled ? ct.Register(() => tcs.TrySetCanceled()) : default;

			TryEnqueue(Execute);

			return await tcs.Task.ConfigureAwait(false);

			async void Execute()
			{
				try
				{
					tcs.TrySetResult(await action(ct).ConfigureAwait(false));
				}
				catch (Exception error)
				{
					tcs.TrySetException(error);
					throw;
				}
			}
		}
	}
}
