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
using Uno.Extensions.Reactive.Testing;

namespace Uno.Extensions.Reactive.Tests.Core;

[TestClass]
public class Given_SequentialRequestManager : FeedTests
{
	[TestMethod]
	public async Task When_AutoPublishInitial()
	{
		var initial = new TestToken(this, 0, 0);
		using var ct = CancellationTokenSource.CreateLinkedTokenSource(CT);
		ct.CancelAfter(TimeSpan.FromMilliseconds(500)); // Safety

		var sut = new SequentialRequestManager<TestRequest, TestToken>(Context.SourceContext, initial, CT, autoPublishInitial: true);

		(await sut.FirstAsync(ct.Token)).Tokens.Single().Should().Be(initial);
	}

	[TestMethod]
	public async Task When_CancelCt_Then_CompleteEnumeration()
	{
		var initial = new TestToken(this, 0, 0);
		using var ct = CancellationTokenSource.CreateLinkedTokenSource(CT);
		ct.CancelAfter(TimeSpan.FromMilliseconds(500)); // Safety

		var sut = new SequentialRequestManager<TestRequest, TestToken>(Context.SourceContext, initial, ct.Token, autoPublishInitial: true);

		var task = sut.ToArrayAsync(CancellationToken.None);

		ct.Cancel();

		task.AsTask().Status.Should().Be(TaskStatus.RanToCompletion);
	}

	[TestMethod]
	public void When_SendMultipleRequest_Then_NewTokenCreatedForEach()
	{
		var initial = new TestToken(this, 0, 0);
		using var requests = new RequestSource();
		var sut = new SequentialRequestManager<TestRequest, TestToken>(Context.SourceContext.CreateChild(requests), initial, CT, autoPublishInitial: true);

		var req1 = new TestRequest();
		var req2 = new TestRequest();

		requests.Send(req1);
		requests.Send(req2);

		req1.Tokens.Single().SequenceId.Should().Be(initial.SequenceId + 1);
		req2.Tokens.Single().SequenceId.Should().Be(initial.SequenceId + 2);
	}

	[TestMethod]
	public void When_Enumerate_Then_NewTokenReceivedForEachRequest()
	{
		var initial = new TestToken(this, 0, 0);
		using var requests = new RequestSource();
		var sut = new SequentialRequestManager<TestRequest, TestToken>(Context.SourceContext.CreateChild(requests), initial, CT, autoPublishInitial: true);

		var result = new List<TokenSet<TestToken>>();
		_ = sut.ForEachAsync(result.Add, CT);

		result.Count.Should().Be(1);

		requests.Send(new TestRequest());

		result.Count.Should().Be(2);

		requests.Send(new TestRequest());

		result.Count.Should().Be(3);
	}


	private record TestRequest : IContextRequest<TestToken>
	{
		public List<TestToken> Tokens { get; } = new();

		/// <inheritdoc />
		public void Register(TestToken token)
			=> Tokens.Add(token);
	}

	private record TestToken(object Source, uint RootContextId, uint SequenceId) : IToken<TestToken>
	{
		/// <inheritdoc />
		public TestToken Next()
			=> this with { SequenceId = SequenceId + 1 };
	}
}
