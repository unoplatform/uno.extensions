using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Sources;

namespace Uno.Extensions.Reactive.Tests.Core;

[TestClass]
public class Given_TokenSetAwaiter
{
	[TestMethod]
	public async Task When_WaitForSync()
	{
		using var ct = new CancellationTokenSource();

		var sut = new TokenSetAwaiter<TestToken>();
		var isCompleted = false;
		sut.WaitFor(Get((0, 42), (1, 42)), () => isCompleted = true);

		isCompleted.Should().BeFalse();

		sut.Received(Get((0, 1), (1, 1)));

		isCompleted.Should().BeFalse();

		sut.Received(Get((0, 1), (1, 42)));

		isCompleted.Should().BeFalse();

		sut.Received(Get((0, 45), (1, 42)));

		isCompleted.Should().BeTrue();
	}

	[TestMethod]
	public async Task When_WaitForAsync()
	{
		using var ct = new CancellationTokenSource();

		var sut = new TokenSetAwaiter<TestToken>();
		var result = sut.WaitFor(Get((0, 42), (1, 42)), ct.Token);

		result.Status.Should().Be(TaskStatus.WaitingForActivation);

		sut.Received(Get((0, 1), (1, 1)));

		result.Status.Should().Be(TaskStatus.WaitingForActivation);

		sut.Received(Get((0, 1), (1, 42)));

		result.Status.Should().Be(TaskStatus.WaitingForActivation);

		sut.Received(Get((0, 45), (1, 42)));

		result.Status.Should().Be(TaskStatus.RanToCompletion);
	}

	[TestMethod]
	public async Task When_DisposeWhileWaitForSync_Then_Complete()
	{
		using var ct = new CancellationTokenSource();

		var sut = new TokenSetAwaiter<TestToken>();
		var isCompleted = false;
		sut.WaitFor(Get((0, 42), (1, 42)), () => isCompleted = true);

		isCompleted.Should().BeFalse();

		sut.Dispose();

		isCompleted.Should().BeTrue();
	}

	[TestMethod]
	public async Task When_DisposeWhileWaitForAsync_Then_Complete()
	{
		using var ct = new CancellationTokenSource();

		var sut = new TokenSetAwaiter<TestToken>();
		var result = sut.WaitFor(Get((0, 42), (1, 42)), ct.Token);

		result.Status.Should().Be(TaskStatus.WaitingForActivation);

		sut.Dispose();

		result.Status.Should().Be(TaskStatus.RanToCompletion);
	}

	[TestMethod]
	public async Task When_WaitForSync_Then_CompleteActionInvokedOnlyOnce()
	{
		using var ct = new CancellationTokenSource();

		var sut = new TokenSetAwaiter<TestToken>();
		var count = 0;
		var result = sut.WaitFor(Get((0, 42), (1, 42)), () => count++);

		sut.Received(Get((0, 42), (1, 42)));
		sut.Received(Get((0, 42), (1, 42)));
		sut.Received(Get((0, 42), (1, 43)));
		sut.Received(Get((0, 43), (1, 43)));

		count.Should().Be(1);
	}

	private TokenSet<TestToken> Get(params (uint ctx, uint seq)[] seqs) => new(seqs.Select(v => new TestToken(this, v.ctx, v.seq)).ToImmutableList());

	private TokenSet<TestToken> Get(uint ctx, uint seq) => new(ImmutableList.Create(new TestToken(this, ctx, seq)));

	private TokenSet<TestToken> Get(object source, uint ctx, uint seq) => new(ImmutableList.Create(new TestToken(source, ctx, seq)));

	private record TestToken(object Source, uint RootContextId, uint SequenceId) : IToken;
}
