#nullable enable

using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Testing;
using Uno.Extensions.Reactive;

namespace Uno.Extensions.Reactive.Tests.Sources
{
	[TestClass]
	public class Given_AsyncFeed : FeedTests
	{
		[TestMethod]
		public async Task When_ProviderReturnsValueSync_Then_GetSome()
		{
			var sut = new AsyncFeed<int>(async _ => 42);
			using var result = await sut.Record();

			result.Should().Be(r => r
				.Message(Changed.Data, 42, Error.No, Progress.Final)
			);
		}

		[TestMethod]
		public async Task When_ProviderReturnsValueAsync_Then_GetSome()
		{
			using var result = await F<int>.Record(r => new AsyncFeed<int>(async ct =>
			{
				await r.WaitForMessages(1, ct);
				return 42;
			}));

			result.Should().Be(r => r
				.Message(Changed.Progress, Data.Undefined, Error.No, Progress.Transient)
				.Message(Changed.Data & Changed.Progress, 42, Error.No, Progress.Final)
			);
		}

		[TestMethod]
		public async Task When_ProviderReturnsDefaultSync_Then_GetNone()
		{
			var sut = new AsyncFeed<int>(async _ => Option.None<int>());
			using var result = await sut.Record();

			result.Should().Be(r => r
				.Message(Changed.Data, Data.None, Error.No, Progress.Final)
			);
		}

		[TestMethod]
		public async Task When_ProviderReturnsSomeAsync_Then_GetValue()
		{
			using var result = await F<int>.Record(r => new AsyncFeed<int>(async ct =>
			{
				await r.WaitForMessages(1, ct);
				return Option.None<int>();
			}));

			result.Should().Be(r => r
				.Message(Changed.Progress, Data.Undefined, Error.No, Progress.Transient)
				.Message(Changed.Data & Changed.Progress, Data.None, Error.No, Progress.Final)
			);
		}

		[TestMethod]
		public async Task When_ProviderThrowsSync_Then_GetError()
		{
			var sut = new AsyncFeed<int>(async _ => throw new TestException());
			using var result = await sut.Record();

			result.Should().Be(r => r
				.Message(Changed.Error, Data.Undefined, typeof(TestException), Progress.Final)
			);
		}

		[TestMethod]
		public async Task When_ProviderThrowsAsync_Then_GetError()
		{
			using var result = await F<int>.Record(r => new AsyncFeed<int>(async ct =>
			{
				await r.WaitForMessages(1, ct);
				throw new TestException();
			}));

			result.Should().Be(r => r
				.Message(Changed.Progress, Data.Undefined, Error.No, Progress.Transient)
				.Message(Changed.Error & Changed.Progress, Data.Undefined, typeof(TestException), Progress.Final)
			);
		}
		

		[TestMethod]
		public async Task When_ProviderReturnsValueSyncAndRefresh_Then_GetSome()
		{
			var refresh = new Signal();
			var sut = new AsyncFeed<int>(async _ => 42, refresh);
			using var result = sut.Record();

			refresh.Raise();
			refresh.Dispose();

			await result;
			result.Should().Be(r => r
				.Message(Changed.Data, 42, Error.No, Progress.Final)
			);
		}

		[TestMethod]
		public async Task When_ProviderReturnsValueAsyncAndRefresh_Then_GetSome()
		{
			var ct = TestContext.CancellationTokenSource.Token;
			var refresh = new Signal();
			var gate = new TaskCompletionSource();
			var sut = new AsyncFeed<int>(
				async _ =>
				{
					await gate.Task;
					return 42;
				}, 
				refresh);
			using var result = sut.Record();

			await result.WaitForMessages(1, ct);
			gate.SetResult();

			await result.WaitForMessages(2, ct);
			gate = new TaskCompletionSource();
			refresh.Raise();
			refresh.Dispose();

			await result.WaitForMessages(3, ct);
			gate.SetResult();

			await result;
			result.Should().Be(r => r
				.Message(Changed.Progress, Data.Undefined, Error.No, Progress.Transient)
				.Message(Changed.Data & Changed.Progress, 42, Error.No, Progress.Final)
				.Message(Changed.Progress, 42, Error.No, Progress.Transient)
				.Message(Changed.Progress, 42, Error.No, Progress.Final)
			);
		}

		[TestMethod]
		public async Task When_ProviderReturnsDefaultSyncAndRefresh_Then_GetNone()
		{
			var refresh = new Signal();
			var sut = new AsyncFeed<int>(async _ => Option.None<int>());
			using var result = sut.Record();

			refresh.Raise();
			refresh.Dispose();

			await result;
			result.Should().Be(r => r
				.Message(Changed.Data, Data.None, Error.No, Progress.Final)
			);
		}

		[TestMethod]
		public async Task When_ProviderReturnsSomeAsyncAndRefresh_Then_GetValue()
		{
			var ct = TestContext.CancellationTokenSource.Token;
			var refresh = new Signal();
			var gate = new TaskCompletionSource();
			var sut = new AsyncFeed<int>(
				async _ =>
				{
					await gate.Task;
					return Option.None<int>();
				},
				refresh);
			using var result = sut.Record();

			await result.WaitForMessages(1, ct);
			gate.SetResult();

			await result.WaitForMessages(2, ct);
			gate = new TaskCompletionSource();
			await Task.Delay(10, ct);
			refresh.Raise();
			refresh.Dispose();

			await result.WaitForMessages(3, ct);
			gate.SetResult();

			await result;
			result.Should().Be(r => r
				.Message(Changed.Progress, Data.Undefined, Error.No, Progress.Transient)
				.Message(Changed.Data & Changed.Progress, Data.None, Error.No, Progress.Final)
				.Message(Changed.Progress, Data.None, Error.No, Progress.Transient)
				.Message(Changed.Progress, Data.None, Error.No, Progress.Final)
			);
		}

		[TestMethod]
		public async Task When_ProviderThrowsSyncAndRefresh_Then_GetError()
		{
			var refresh = new Signal();
			var sut = new AsyncFeed<int>(async _ => throw new TestException());
			using var result = sut.Record();

			refresh.Raise();
			refresh.Dispose();

			await result;
			result.Should().Be(r => r
				.Message(Data.Undefined, typeof(TestException), Progress.Final)
			);
		}

		[TestMethod]
		public async Task When_ProviderThrowsAsyncAndRefresh_Then_GetError()
		{
			var ct = TestContext.CancellationTokenSource.Token;
			var refresh = new Signal();
			var gate = new TaskCompletionSource();
			var sut = new AsyncFeed<int>(
				async _ =>
				{
					await gate.Task;
					throw new TestException();
				},
				refresh);
			using var result = sut.Record();

			await result.WaitForMessages(1, ct);
			gate.SetResult();

			await result.WaitForMessages(2, ct);
			gate = new TaskCompletionSource();
			refresh.Raise();
			refresh.Dispose();

			await result.WaitForMessages(3, ct);
			gate.SetResult();

			await result;
			result.Should().Be(r => r
				.Message(Changed.Progress, Data.Undefined, Error.No, Progress.Transient)
				.Message(Changed.Error & Changed.Progress, Data.Undefined, typeof(TestException), Progress.Final)
				.Message(Changed.Progress, Data.Undefined, typeof(TestException), Progress.Transient)
				.Message(Changed.Error & Changed.Progress, Data.Undefined, typeof(TestException), Progress.Final)
			);
		}

		[TestMethod]
		public async Task When_ProviderReturnSomeThenThrows_Then_KeepPreviousData()
		{
			var ct = TestContext.CancellationTokenSource.Token;
			var refresh = new Signal();
			var shouldThrow = false;
			var sut = new AsyncFeed<int>(
				async _ =>
				{
					if (shouldThrow)
					{
						throw new TestException();
					}
					else
					{
						return 42;
					}
				},
				refresh);
			using var result = sut.Record();

			await result.WaitForMessages(1, ct);
			shouldThrow = true;
			refresh.Raise();

			await result.WaitForMessages(2, ct);
			shouldThrow = false;
			refresh.Raise();

			await result.WaitForMessages(3, ct);
			refresh.Dispose();

			await result;
			result.Should().Be(r => r
				.Message(Changed.Data, 42, Error.No, Progress.Final)
				.Message(Changed.Error, 42, typeof(TestException), Progress.Final)
				.Message(Changed.Error, 42, Error.No, Progress.Final)
			);
		}

		[TestMethod]
		public async Task When_NoContext_Then_NoCaching()
		{
			Context.ResignCurrent();

			var invokeCount = 0;
			var sut = new AsyncFeed<int>(async _ =>
			{
				invokeCount++;
				return Option.None<int>();
			});

			await sut;
			await sut;

			invokeCount.Should().Be(2);
		}

		[TestMethod]
		public async Task When_InvokeProvider_Then_ContextSet()
		{
			Context.ResignCurrent(); // We prefer to explicitly give it in GetSource to avoid any uncontrolled flowing

			var sut = new AsyncFeed<int>(async _ =>
			{
				SourceContext.Current.Should().Be(Context.SourceContext);
				return Option.None<int>();
			});
			using var result = await sut.Record(Context.SourceContext);

			result.Should().Be(r => r
				.Message(Error.No)
			);
		}
	}
}
