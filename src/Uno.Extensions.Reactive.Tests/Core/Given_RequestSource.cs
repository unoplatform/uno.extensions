using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Testing;

namespace Uno.Extensions.Reactive.Tests.Core;

[TestClass]
public class Given_RequestSource : FeedTests
{
	[TestMethod]
	public async Task When_Send_Then_Forwarded()
	{
		using var sut = new RequestSource();
		var rq = new TestRequest();

		var resultTask = Take(sut, 1, CT);

		sut.Send(rq);

		var result = await resultTask;

		result.Should().BeEquivalentTo(new[] { rq });
	}

	[TestMethod]
	public async Task When_Disposed_Then_EnumerationCompletes()
	{
		var sut = new RequestSource();

		var resultTask = Take(sut, 128, CT);

		sut.Dispose();

		resultTask.IsCompleted.Should().BeTrue();
	}

	[TestMethod]
	public async Task When_DisposedAndSend_Then_Throws()
	{
		var sut = new CompositeRequestSource();
		var rq = new TestRequest();

		sut.Dispose();
		Assert.ThrowsExactly<ObjectDisposedException>(() => sut.Send(rq));
	}

	private Task<List<IContextRequest>> Take(RequestSource sut, int count, CancellationToken ct)
	{
		var result = new List<IContextRequest>(count);
		var tcs = new TaskCompletionSource<List<IContextRequest>>();
		sut.RequestRaised += OnRequest;
		ct.Register(Complete);

		return tcs.Task;

		void OnRequest(object? sender, IContextRequest req)
		{
			result.Add(req);
			if (--count is 0 || req is EndRequest)
			{
				Complete();
			}
		}

		void Complete()
		{
			sut.RequestRaised -= OnRequest;
			tcs.TrySetResult(result);
		}
	}
}
