#nullable disable // Imported from Uno.Core

// ******************************************************************
// Copyright � 2015-2018 nventive inc. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// ******************************************************************
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions;
using Uno.Extensions.Reactive;

namespace Uno.Core.Tests.TestUtils
{
	public class AsyncTestRunner : IDisposable
	{
#if DEBUG
		private static int _beatTimeout => Debugger.IsAttached ? 10 * 1000 : 100;
#else
		private const int _beatTimeout = 100;
#endif

		private readonly bool _loop;
		private static ImmutableList<AsyncTestRunner> _runners = ImmutableList<AsyncTestRunner>.Empty;

		private readonly CancellationTokenSource _ct = new CancellationTokenSource();
		private readonly AutoResetEvent _syncEvent = new AutoResetEvent(initialState: false);
		private readonly AutoResetEvent _queueEvent = new AutoResetEvent(initialState: false);
		private readonly ManualResetEventSlim _exited = new ManualResetEventSlim(initialState: false);

		private readonly string _identifier;

		private ImmutableQueue<QueueItem> _queue = ImmutableQueue<QueueItem>.Empty;
		private QueueItem _current;
		private Thread _thread;
		private bool _isStopped;
		private SyncFlag _syncFlag;
		private int _syncPosition;
		private ImmutableDictionary<string , object> _interopValues = ImmutableDictionary<string, object>.Empty;

		public AsyncTestRunner(AsyncAction<AsyncTestRunner> method = null, [CallerMemberName] string name = null, [CallerLineNumber] int line = -1, bool loop = false)
		{
			_loop = loop;
			_identifier = $"{name}@{line}";
			ImmutableInterlocked.Update(ref _runners, r => r.Add(this));

			if (method != null)
			{
				Run(method);
			}
		}

		public int ThreadId => _thread.ManagedThreadId;

		public T Get<T>(string key, T defaultValue = default(T)) => _interopValues.TryGetValue(key, out var value) ? (T)value : defaultValue;

		public void Set<T>(string key, T value) => ImmutableInterlocked.Update(ref _interopValues, values => values.SetItem(key, value));

		public Task Run(AsyncAction<AsyncTestRunner> method)
		{
			var item = new QueueItem(method);
			ImmutableInterlocked.Enqueue(ref _queue, item);

			if (_thread == null)
			{
				var thread = new Thread(RunLoop);
				if (Interlocked.CompareExchange(ref _thread, thread, null) == null)
				{
					thread.Start();
				}
			}
			else
			{
				_queueEvent.Set();
			}

			return item.Task;
		}

		private async void RunLoop()
		{
			try
			{
				while (!_isStopped)
				{
					while (ImmutableInterlocked.TryDequeue(ref _queue, out _current))
					{
						try
						{
							SyncCore();
							await _current.Run(this);

							Interlocked.Exchange(ref _interopValues, ImmutableDictionary<string, object>.Empty);
							Interlocked.Exchange(ref _syncFlag, null)?.Reached(int.MaxValue);
							Interlocked.Exchange(ref _syncPosition, 0);
						}
						catch (ObjectDisposedException ode) when (ode.ObjectName == nameof(AsyncTestRunner))
						{
						}
						catch (OperationCanceledException)
						{
						}
						catch (Exception e)
						{
							foreach (var runner in _runners.Except(new [] {this}))
							{
								runner.Abort($"****ANOTHER**** Test runner ({_identifier}) failed with: {e.Message}");
							}

							Abort(e);

							throw;
						}
					}

					if (_isStopped || !_loop)
					{
						return;
					}

					_queueEvent.WaitOne();
				}
			}
			finally
			{
				_exited.Set();
				Dispose();
			}
		}

		public void Sync() => Sync(_syncPosition +1);

		public void Sync(int position)
		{
			_current?.Beat();

			var previousPosition = Interlocked.Exchange(ref _syncPosition, position);
			if (previousPosition > position)
			{
				throw new InvalidOperationException("Sync index is lower than the previous");
			}

			var currentFlag = _syncFlag;
			if (currentFlag == null
				|| position < currentFlag.Position)
			{
				// continue
			}
			else if (position == currentFlag.Position)
			{
				currentFlag.Reached(position);

				SyncCore();
			}
			else //if (position > currentFlag.Position)
			{
				throw new InvalidOperationException("We missed a sync flag!");
			}
		}

		private void SyncCore()
		{
			_current?.Beat();

			_syncEvent.WaitOne();

			if (_isStopped)
			{
				// Make sure the works does not continue
				throw new ObjectDisposedException(nameof(AsyncTestRunner));
			}

			_current?.Beat();
		}

		public Task IsFrozen() => _current?.Frozen();

		public Task Advance() => AdvanceTo(_syncPosition + 1);

		public Task AdvanceToEnd() => AdvanceTo(int.MaxValue);

		public Task AdvanceTo(int position)
		{
			var flag = new SyncFlag(position);
			Interlocked.Exchange(ref _syncFlag, flag)?.Canceled();
			if (position == _syncPosition)
			{
				return Task.CompletedTask;
			}
			else if (position < _syncPosition)
			{
				throw new InvalidOperationException("Thread is already at positon " + _syncPosition);
			}

			_syncEvent.Set();

			return flag.Wait();
		}

		public async Task AdvanceAndFreezeBefore(int position)
		{
			var flag = new SyncFlag(position);
			Interlocked.Exchange(ref _syncFlag, flag)?.Canceled();
			if (position <= _syncPosition)
			{
				throw new InvalidOperationException("Thread is already at positon " + _syncPosition);
			}

			_syncEvent.Set();

			var reached = flag.Wait();
			var frozen = _current.Frozen();

			await Task.WhenAny(reached, frozen);

			Assert.AreEqual(TaskStatus.WaitingForActivation, reached.Status);
			Assert.AreEqual(TaskStatus.RanToCompletion, frozen.Status);
		}

		private void Abort(object reason)
		{
			AbortSafe(reason as Exception ?? new Exception(reason.ToString()));

			_thread.Interrupt();
			_thread.Join(1000);
			_exited.Set();
		}

		private void AbortSafe(Exception reason)
		{
			if (_isStopped)
			{
				return;
			}

			ImmutableInterlocked.Update(ref _runners, r => r.Remove(this));

			try { _syncEvent.Set(); } catch (ObjectDisposedException) { }
			try { _queueEvent.Set(); } catch (ObjectDisposedException) { }

			try { _syncEvent.Dispose(); } catch (ObjectDisposedException) { }
			try { _queueEvent.Dispose(); } catch (ObjectDisposedException) { }

			try { _ct.Cancel(); } catch (ObjectDisposedException) { }
			try { _ct.Dispose(); } catch (ObjectDisposedException) { }

			_current?.Fail(reason);
			_syncFlag?.Failed(reason);
			foreach (var item in _queue)
			{
				item.Abort();
			}

			_isStopped = true;
		}

		public void Dispose()
		{
			AbortSafe(new ObjectDisposedException(nameof(AsyncTestRunner)));

			if (_thread.IsAlive)
			{
				_exited.Wait();
			}
		}

		private class QueueItem
		{
			private readonly AsyncAction<AsyncTestRunner> _method;
			private readonly TaskCompletionSource<Unit> _task = new TaskCompletionSource<Unit>();

			private DateTime _lastBeat = DateTime.Now;

			public QueueItem(AsyncAction<AsyncTestRunner> method)
			{
				_method = method;
			}

			public void Beat()
			{
				_lastBeat = DateTime.Now;
			}

			public async Task Frozen()
			{
				var delay = _lastBeat.AddMilliseconds(_beatTimeout) - DateTime.Now;
				if (delay > TimeSpan.Zero)
				{
					await Task.Delay(delay);
				}
				else
				{
					await Task.Yield();
				}
			}

			public async Task Run(AsyncTestRunner runner)
			{
				try
				{
					await _method(runner, runner._ct.Token);
					_task.TrySetResult(Unit.Default);
				}
				catch (Exception e)
				{
					_task.TrySetException(e);

					throw;
				}
			}

			public void Fail(Exception e) => _task.TrySetException(e);

			public void Abort() => _task.TrySetCanceled();

			public Task Task => _task.Task;
		}

		private class SyncFlag
		{
			private readonly TaskCompletionSource<int> _task = new TaskCompletionSource<int>();

			public SyncFlag(int position)
			{
				Position = position;
			}

			public int Position { get; }

			public Task Wait() => _task.Task;

			public void Reached(int position) => Task.Run(() => _task.TrySetResult(position)); // Ensure that awaiter does not run on this thread!

			public void Failed(Exception e) => _task.TrySetException(e);

			public void Canceled() => _task.TrySetCanceled();
		}
	}
}
