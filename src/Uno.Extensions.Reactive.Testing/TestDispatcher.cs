using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Uno.Extensions.Reactive.Testing;

/// <summary>
/// An implementation of <see cref="IDispatcher"/> that can be used to abstract the UI thread in tests.
/// </summary>
public sealed class TestDispatcher : IDispatcher, IDisposable
{
	private readonly Thread _thread;
	private readonly Queue<Action> _queue = new();
	private readonly AutoResetEvent _evt = new(false);

	private bool _isDisposed;

	/// <summary>
	/// Creates a new instance.
	/// </summary>
	public TestDispatcher(string? testName = null)
	{
		Name = testName ?? "testDispatcher";
		_thread = new Thread(Run) { Name =  Name };
		_thread.Start();
	}

	/// <summary>
	/// Gets the name of the dispatcher thread.
	/// </summary>
	public string Name { get; }

	/// <inheritdoc />
	public bool HasThreadAccess => Thread.CurrentThread == _thread;

	/// <inheritdoc />
	public bool TryEnqueue(Action action)
	{
		if (_isDisposed)
		{
			// For tests we prefer to throw instead of returning false in order to make clear the invalid usage.
			throw new InvalidOperationException("Dispatcher has already been aborted!");
		}

		lock (_queue)
		{
			_queue.Enqueue(action);
		}

		_evt.Set();
		return true;
	}

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

	private void Run()
	{
		while (!_isDisposed)
		{
			try
			{
				bool hasItem;
				Action? item;
				lock (_queue)
				{
					hasItem = _queue.Count > 0;
					item = hasItem ? _queue.Dequeue() : default;
				}

				if (hasItem)
				{
					item!();
				}
				else
				{
					_evt.WaitOne();
				}
			}
			catch (Exception error)
			{
				throw new InvalidOperationException("Got an exception on the UI thread", error);
			}
		}
	}

	public void Dispose()
	{
		_isDisposed = true;
		_evt.Set();
		_thread.Join();
	}
}
