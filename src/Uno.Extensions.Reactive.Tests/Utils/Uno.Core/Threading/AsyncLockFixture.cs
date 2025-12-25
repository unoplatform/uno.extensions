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
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Core.Tests.TestUtils;
using Uno.Extensions;
using Uno.Extensions.Reactive;
using Uno.Extensions.Threading;

#pragma warning disable MSTEST0045 // Use 'CooperativeCancellation = true' with '[Timeout]' - the tests need to be modified to be friendly to cooperative cancellation.
// NOTE: Cooperative cancellation is when MSTest requests cancellation for the TestContext.CancellationToken when the timeout happens, and the test is responsible to terminate when it receives cancellation.
// Non-cooperative cancellation is when MSTest just keeps the test method running but stops observing it and considers it failing.

namespace Uno.Core.Tests.Threading
{
	[TestClass]
	[Ignore("Flaky tests")]
	public class AsyncLockFixture
	{
		public TestContext TestContext { get; set; }

#if DEBUG
		private const int _timeout = int.MaxValue;
#else
		private const int _timeout = 5000;
#endif

		[TestMethod]
		[Timeout(_timeout)]
		[Ignore] // https://github.com/nventive/Uno.Core/issues/48
		public async Task TestUnlockReleaseNextSynchronously()
		{
			Console.WriteLine($"Running on thread {Thread.CurrentThread.ManagedThreadId}");

			var sut = new FastAsyncLock();
			var thread1ExitingThread = -1;
			var thread2LockingTask = default(Task<IDisposable>);
			var thread1 = new AsyncTestRunner(Thread1);
			var thread2 = new AsyncTestRunner();
			_ = thread2.Run(Thread2);

			using (thread1)
			using (thread2)
			{
				// Acquire the lock on thread 1
				await thread1.AdvanceTo(1);

				// Wait for the thread 2 to be stuck to acquire the lock
				var thread2Locking = thread2.AdvanceTo(1);
				while (thread2LockingTask == null)
				{
					await Task.Yield();
				}

				// Make sure that the thread 2 is really awaiting the thread2LockingTask before requesting thread1 to continue
				await Task.Delay(100);
				Assert.AreEqual(TaskStatus.WaitingForActivation, thread2LockingTask.Status);

				// Relase the thread 1 and make sure that the thread 2 is able to acquire the lock
				var thread1Release = thread1.AdvanceTo(2);
				await thread2Locking;
			}

			async ValueTask Thread1(AsyncTestRunner r, CancellationToken ct)
			{
				Console.WriteLine($"Thread 1: {Thread.CurrentThread.ManagedThreadId}");

				var ctx = AsyncTestContext.Current;
				using (await sut.LockAsync(ct))
				{
					Console.WriteLine($"Acquired lock for Thread1 on thread: {Thread.CurrentThread.ManagedThreadId}");

					ctx.Validate();
					await Task.Yield();
					r.Sync(position: 1);

					thread1ExitingThread = Thread.CurrentThread.ManagedThreadId;
					Console.WriteLine($"Releasing lock from Thread1 on thread: {Thread.CurrentThread.ManagedThreadId}");
				}

				Console.WriteLine($"Released lock from Thread1 on thread: {Thread.CurrentThread.ManagedThreadId}");

				// This must have run synchronously when lock got released (disposed).
				Assert.AreEqual(TaskStatus.RanToCompletion, thread2LockingTask.Status);

				r.Sync(position: 2);
			}

			async ValueTask Thread2(AsyncTestRunner r, CancellationToken ct)
			{
				Console.WriteLine($"Thread 2: {Thread.CurrentThread.ManagedThreadId}");

				var ctx = AsyncTestContext.Current;

				thread2LockingTask = sut.LockAsync(ct);

				// Validate that we are running on thread 2
				Assert.AreEqual(thread2.ThreadId, Thread.CurrentThread.ManagedThreadId);

				Console.WriteLine("Thread 2 is waiting for lock");
				using (await thread2LockingTask)
				{
					Console.WriteLine($"Acquired lock for Thread2 on thread: {Thread.CurrentThread.ManagedThreadId}");

					// Here we should run on the thread 1 since the lock is released synchronously from thread 1
					Assert.AreEqual(thread1ExitingThread, Thread.CurrentThread.ManagedThreadId);

					// But we should have kept the ExecutionContext from the thread 2
					ctx.Validate();

					await Task.Yield();
					r.Sync(position: 1);

					Console.WriteLine($"Releasing lock from Thread2 on thread: {Thread.CurrentThread.ManagedThreadId}");
				}

				Console.WriteLine($"Released lock from Thread2 on thread: {Thread.CurrentThread.ManagedThreadId}");

				r.Sync(position: 2);
			}
		}

		[TestMethod]
		[Timeout(_timeout)]
		public async Task TestConcurrentAccess()
		{
			var entryContext = AsyncTestContext.Current;
			var sut = new FastAsyncLock();

			using (var otherThread = new AsyncTestRunner(CommonTwoStepsLock(sut)))
			{
				// Acquire the lock on another async context
				await otherThread.Advance(); 
				Assert.IsTrue(otherThread.HasLock());

				// Try to acquire the lock from this async context
				entryContext.Validate();
				var locking = sut.LockAsync(CancellationToken.None);
				Assert.AreEqual(TaskStatus.WaitingForActivation, locking.Status);

				await otherThread.Advance(); // Will release the lock
				Assert.AreEqual(TaskStatus.RanToCompletion, locking.Status); // so lock is now acquired (sync)
			}
		}

		[TestMethod]
		[Timeout(_timeout)]
		public async Task TestConcurrentCancelSecond()
		{
			var entryContext = AsyncTestContext.Current;
			var sut = new FastAsyncLock();
			var ct = new CancellationTokenSource();

			using (var otherThread = new AsyncTestRunner(CommonTwoStepsLock(sut)))
			{
				// Acquire the lock on another async context
				await otherThread.Advance();
				Assert.IsTrue(otherThread.HasLock());

				// Try to acquire the lock from this async context
				entryContext.Validate();
				var locking = sut.LockAsync(ct.Token);
				Assert.AreEqual(TaskStatus.WaitingForActivation, locking.Status);

				// But cancel before the other async context completes
				ct.Cancel();
				Assert.AreEqual(TaskStatus.Canceled, locking.Status);
			}
		}

		[TestMethod]
		[Timeout(_timeout)]
		public async Task TestConcurrentCancelFirst()
		{
			var entryContext = AsyncTestContext.Current;
			var sut = new FastAsyncLock();
			var ct = new CancellationTokenSource();

			using (var otherThread = new AsyncTestRunner(CommonTwoStepsLock(sut)))
			{
				// Acquire the lock on another async context
				await otherThread.Advance();
				Assert.IsTrue(otherThread.HasLock());

				// Try to acquire the lock from this async context
				entryContext.Validate();
				var locking = sut.LockAsync(CancellationToken.None);
				Assert.AreEqual(TaskStatus.WaitingForActivation, locking.Status);

				// Cancel the CT of the other thread ... this should not impact the awaiter !
				ct.Cancel();
				Assert.AreEqual(TaskStatus.WaitingForActivation, locking.Status);

				// Finally validate that if we release the lock from the other thread, we are still able to acquire the lock
				await otherThread.Advance();
				Assert.AreEqual(TaskStatus.RanToCompletion, locking.Status);
			}
		}

		[TestMethod]
		[Timeout(_timeout)]
		[Ignore]
		public async Task TestConcurrentCancelSecondWithThird()
		{
			var sut = new FastAsyncLock();
			var ct = new CancellationTokenSource();

			var thread2LockingTask = default(Task<IDisposable>);

			using (var thread1 = new AsyncTestRunner(Thread1))
			using (var thread2 = new AsyncTestRunner(Thread2))
			using (var thread3 = new AsyncTestRunner(Thread3))
			{
				// Acquire the lock on thread 1 THEN 
				await thread1.AdvanceTo(1);
				Assert.IsTrue(thread1.HasLock());

				// Try to acquire the lock from this async context
				await thread2.AdvanceAndFreezeBefore(1);
				//var locking = sut.LockAsync(ct.Token);
				Assert.AreEqual(TaskStatus.WaitingForActivation, thread2LockingTask.Status);
				Assert.IsFalse(thread2.HasLock());

				// Try to acquire it on thread 3 
				var t3Locked = thread3.AdvanceTo(1);
				await thread3.IsFrozen();
				Assert.IsFalse(thread3.HasLock());

				// But cancel before the other async context completes
				ct.Cancel();
				Assert.AreEqual(TaskStatus.Canceled, thread2LockingTask.Status);

				// Release the lock from thread1, and wait for thread 3 to acquire the lock
				await thread1.AdvanceAndFreezeBefore(2); // will freeze in continuation of thread 3
				await t3Locked;

				//Assert.IsFalse(thread1.HasLock()); // flag not set yet: the thread 1 is dead locked by the continuation of thread 3
				Assert.IsTrue(thread3.HasLock());

				await thread3.AdvanceToEnd();
				//await thread1.AdvanceToEnd();
				await Task.Delay(500);

				Assert.IsFalse(thread1.HasLock());
				Assert.IsFalse(thread3.HasLock());
			}


			async ValueTask Thread1(AsyncTestRunner r, CancellationToken ct2)
			{
				using (await sut.LockAsync(ct2))
				{
					r.HasLock(true);
					r.Sync(position: 1);
				}

				r.HasLock(false);
				r.Sync(position: 2);
			};

			async ValueTask Thread2(AsyncTestRunner r, CancellationToken ct2)
			{
				thread2LockingTask = sut.LockAsync(ct.Token);
				using (await thread2LockingTask)
				{
					r.HasLock(true);
					r.Sync(position: 1);
				}

				r.HasLock(false);
				r.Sync(position: 2);
			}

			async ValueTask Thread3(AsyncTestRunner r, CancellationToken ct2)
			{
				using (await sut.LockAsync(ct2))
				{
					r.HasLock(true);
					r.Sync(position: 1);
				}

				r.HasLock(false);
				r.Sync(position: 2);
			};
		}

		[TestMethod]
		[Timeout(_timeout)]
		public async Task TestReEntrencySync()
		{
			var sut = new FastAsyncLock();
			using (await sut.LockAsync(CancellationToken.None))
			{
				using (await sut.LockAsync(CancellationToken.None))
				{
				}
			}
		}

		[TestMethod]
		[Timeout(_timeout)]
		public async Task TestReEntrencyAsync()
		{
			var sut = new FastAsyncLock();
			using (await sut.LockAsync(CancellationToken.None))
			{
				await Task.Yield();

				using (await sut.LockAsync(CancellationToken.None))
				{
					await Task.Yield();
				}
			}
		}

		[TestMethod]
		[Timeout(_timeout)]
		public async Task TestReEntrencyWithConcurrentAccess()
		{
			var sut = new FastAsyncLock();
			using (var thread2 = new AsyncTestRunner(Thread2))
			using (var thread1 = new AsyncTestRunner(Thread1))
			{

				// Enter from main thread
				await thread1.AdvanceTo(1);

				// Try enter from second thread
				var thread2Locking = thread2.AdvanceTo(2);
				await Task.Delay(100);
				Assert.AreEqual(TaskStatus.WaitingForActivation, thread2Locking.Status);

				// ReEnter
				await thread1.AdvanceTo(2);

				// Exit once
				await thread1.AdvanceTo(3);
				Assert.AreEqual(TaskStatus.WaitingForActivation, thread2Locking.Status);

				// Final exit
				await thread1.AdvanceTo(4);
				await thread2Locking;
			}

			async ValueTask Thread1(AsyncTestRunner r, CancellationToken ct)
			{
				using (await sut.LockAsync(CancellationToken.None))
				{
					await Task.Yield();
					r.HasLock(true);
					r.Sync(position: 1);

					using (await sut.LockAsync(CancellationToken.None))
					{
						await Task.Yield();
						r.Sync(position: 2);
					}

					r.Sync(position: 3);
				}

				r.HasLock(false);
				r.Sync(position: 4);
			}

			async ValueTask Thread2(AsyncTestRunner r, CancellationToken ct)
			{
				using (await sut.LockAsync(ct))
				{
					r.HasLock(true);
					r.Sync(position: 1);
				}

				r.HasLock(false);
				r.Sync(position: 2);
			}
		}

		[TestMethod]
		[Timeout(_timeout)]
		public async Task TestReleaseThenReAcquire()
		{
			var sut = new FastAsyncLock();
			using (await sut.LockAsync(CancellationToken.None))
			{
				await Task.Yield();
			}

			await Task.Yield();

			using (await sut.LockAsync(CancellationToken.None))
			{
				await Task.Yield();
			}
		}

		[TestMethod]
		[Timeout(_timeout)]
		[Ignore] // https://github.com/unoplatform/Uno.Core/issues/59
		public async Task TestReleaseThenReAcquireWithConcurrentAccess()
		{
			var sut = new FastAsyncLock();
			using (var otherThread = new AsyncTestRunner(OtherThread))
			{
				await otherThread.AdvanceTo(1);
				Assert.IsTrue(otherThread.HasLock());

				await otherThread.AdvanceTo(2);
				Assert.IsFalse(otherThread.HasLock());

				using (await sut.LockAsync(CancellationToken.None))
				{
					await Task.Yield();

					await otherThread.AdvanceAndFreezeBefore(3);
					Assert.IsFalse(otherThread.HasLock());
				}

				await otherThread.AdvanceTo(4);
				Assert.IsTrue(otherThread.HasLock());
			}

			async ValueTask OtherThread(AsyncTestRunner r, CancellationToken ct)
			{
				using (await sut.LockAsync(CancellationToken.None))
				{
					await Task.Yield();
					r.HasLock(true);
					r.Sync(position: 1);
				}

				await Task.Yield();
				r.HasLock(false);
				r.Sync(position: 2);

				using (await sut.LockAsync(CancellationToken.None))
				{
					await Task.Yield();
					r.HasLock(true);
					r.Sync(position: 3);
					r.Sync(position: 4);
				}

				await Task.Yield();
				r.HasLock(false);
			}
		}

		[TestMethod]
		[Timeout(_timeout)]
		public async Task TestMultipleDispose()
		{
			var sut = new FastAsyncLock();

			var handle = await sut.LockAsync(CancellationToken.None);
			handle.Dispose();
			handle.Dispose();

			// Validate that we can still acquire the lock
			using (await sut.LockAsync(CancellationToken.None))
			{
			}
		}

		[TestMethod]
		[Timeout(_timeout)]
		[Ignore]
		public async Task TestExitFromAnotherExecutionContext()
		{
			var sut = new FastAsyncLock();
			var locking = default(IDisposable);
			var entryContext = default(AsyncTestContext);

			using (var externalThread = new AsyncTestRunner(ExternalThread))
			{
				entryContext = AsyncTestContext.Current;
				locking = await sut.LockAsync(CancellationToken.None);

				await externalThread.AdvanceTo(1);

				Assert.IsTrue(externalThread.HasLock());

				await externalThread.AdvanceToEnd();
			}

			async ValueTask ExternalThread(AsyncTestRunner r, CancellationToken ct)
			{
				Assert.AreNotEqual(entryContext, AsyncTestContext.Current);
				locking.Dispose();

				// Try to relock from the external execution context to validate that the lock was effectively released
				using (await sut.LockAsync(ct))
				{
					r.HasLock(true);
					r.Sync(position: 1);
					await Task.Yield();
				}

				r.HasLock(false);
			}
		}


		private static AsyncAction<AsyncTestRunner> CommonTwoStepsLock(FastAsyncLock @lock) => async (r, ct) =>
		{
			using (await @lock.LockAsync(ct))
			{
				r.HasLock(true);
				r.Sync(position: 1);
			}

			r.HasLock(false);
			r.Sync(position: 2);
		};
	}

	internal static class RunnerExtensions
	{
		internal static bool HasLock(this AsyncTestRunner runner) => runner.Get<bool>("hasLock");

		internal static void HasLock(this AsyncTestRunner runner, bool value) => runner.Set("hasLock", value);
	}
}
