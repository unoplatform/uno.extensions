using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Win32.SafeHandles;
using Uno.Extensions.Reactive.Bindings;
using Uno.Extensions.Reactive.Dispatching;
using Uno.Extensions.Reactive.Testing;

namespace Uno.Extensions.Reactive.Tests;

public class FeedUITests : FeedTests
{
	private readonly Dispatcher _dispatcher = new();

	/// <inheritdoc />
	[TestInitialize]
	public override void Initialize()
	{
		base.Initialize();

		DispatcherHelper.GetForCurrentThread = () => _dispatcher.HasThreadAccess ? _dispatcher : null;
	}

	/// <inheritdoc />
	[TestCleanup]
	public override void Cleanup()
	{
		_dispatcher.Dispose();

		base.Cleanup();
	}

	private protected async Task<T> WaitForInitialValue<TViewModel, T>(TViewModel viewModel, Func<TViewModel, Bindable<T>> propertySelector, CancellationToken ct = default)
		where TViewModel : BindableViewModelBase
		where T : class
	{
		if (!ct.CanBeCanceled)
		{
			ct = CT;
		}

		var tcs = new TaskCompletionSource<T>();
		await using var _ = ct.Register(() => tcs.TrySetCanceled());
		await ExecuteOnDispatcher(() =>
			{
				var bindable = propertySelector(viewModel);

				// Note: This is a patch since the implementation of IFeed by the bindable is only a VM.SrcFeed.Select(getter).
				//		 It should be a real implementation that synchronously reflects the current value of the bindable itself.
				var currentValue = bindable.GetValue();
				if (currentValue != null)
				{
					// This is a weak test since we could have a feed that init with null. But it's enough for now, regarding the comment above !
					tcs.TrySetResult(currentValue);
					return;
				}

				// Adding the event handler will also init the dispatcher
				var ctReg = default(CancellationTokenRegistration);
				viewModel.PropertyChanged += PropertyChanged;
				ctReg = ct.Register(() => viewModel.PropertyChanged -= PropertyChanged);

				void PropertyChanged(object? sender, PropertyChangedEventArgs propertyChangedEventArgs)
				{
					if (propertyChangedEventArgs.PropertyName == bindable.PropertyName)
					{
						ctReg.Dispose();
						viewModel.PropertyChanged -= PropertyChanged;
						tcs.TrySetResult(bindable.GetValue());
					}
				}
			},
			ct);

		return await tcs.Task;
	}

	private protected async ValueTask ExecuteOnDispatcher(Action action, CancellationToken ct = default)
	{
		if (!ct.CanBeCanceled)
		{
			ct = CT;
		}

		var tcs = new TaskCompletionSource();
		await using var _ = ct.Register(() => tcs.TrySetCanceled());
		_dispatcher.TryEnqueue(() =>
		{
			action();
			tcs.TrySetResult();
		});

		await tcs.Task;
	}

	private protected async ValueTask ExecuteAsyncOnDispatcher(AsyncAction action, CancellationToken ct = default)
	{
		if (!ct.CanBeCanceled)
		{
			ct = CT;
		}

		var tcs = new TaskCompletionSource();
		await using var _ = ct.Register(() => tcs.TrySetCanceled());
		_dispatcher.TryEnqueue(async () =>
		{
			await action(ct);
			tcs.TrySetResult();
		});

		await tcs.Task;
	}

	private class Dispatcher : IDispatcherInternal, IDisposable
	{
		private readonly Thread _thread;
		private readonly Queue<Action> _queue = new();
		private readonly AutoResetEvent _evt = new(false);

		private bool _isDisposed;

		public Dispatcher()
		{
			_thread = new Thread(Run);
			_thread.Start();
		}

		/// <inheritdoc />
		public bool HasThreadAccess => Thread.CurrentThread == _thread;

		/// <inheritdoc />
		public void TryEnqueue(Action action)
		{
			if (_isDisposed)
			{
				throw new InvalidOperationException("Dispatcher has already been aborted!");
			}

			lock (_queue)
			{
				_queue.Enqueue(action);
			}

			_evt.Set();
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
						hasItem = _queue.TryDequeue(out item);
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
}
