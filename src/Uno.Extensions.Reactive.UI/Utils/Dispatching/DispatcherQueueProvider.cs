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
		=> _value.Value;

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
				}
			}
		}
	}
}
