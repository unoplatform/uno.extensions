using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Extensions.Reactive.Dispatching;

internal static class DispatcherQueueProvider
{
//#pragma warning disable CA2255 // The 'ModuleInitializer' attribute should not be used in libraries
//	[ModuleInitializer]
//#pragma warning restore CA2255 // The 'ModuleInitializer' attribute should not be used in libraries
//	public static void Initialize()
//		=> DispatcherHelper.GetForCurrentThread = GetForCurrentThread;

	private static readonly ThreadLocal<IDispatcher?> _value = new(CreateForCurrentThread, false);

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

			return await tcs.Task;

			async void Execute()
			{
				try
				{
					tcs.TrySetResult(await action(ct));
				}
				catch (Exception error)
				{
					tcs.TrySetException(error);
				}
			}
		}
	}
}
