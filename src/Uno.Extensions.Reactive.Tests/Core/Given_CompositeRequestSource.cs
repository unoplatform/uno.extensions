using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Testing;

namespace Uno.Extensions.Reactive.Tests.Core;

[TestClass]
public class Given_CompositeRequestSource : FeedTests
{
	[TestMethod]
	public async Task When_Add_Then_Composed()
	{
		var sut = new CompositeRequestSource();
		var src1 = new RequestSource();
		var src2 = new RequestSource();
		var ct1 = CancellationTokenSource.CreateLinkedTokenSource(CT);
		var ct2 = CancellationTokenSource.CreateLinkedTokenSource(CT);
		var rq1 = new TestRequest();
		var rq2 = new TestRequest();

		var resultTask = Take(sut, 2, CT);
		
		sut.Add(src1, ct1.Token);
		sut.Add(src2, ct2.Token);

		src1.Send(rq1);
		src2.Send(rq2);

		var result = await resultTask;

		result.Should().BeEquivalentTo(new[] { rq1, rq2 });
	}

	[TestMethod]
	public async Task When_AddAndCancel_Then_NoLongerComposed()
	{
		var sut = new CompositeRequestSource();
		var src1 = new RequestSource();
		var src2 = new RequestSource();
		var ct1 = CancellationTokenSource.CreateLinkedTokenSource(CT);
		var ct2 = CancellationTokenSource.CreateLinkedTokenSource(CT);
		var rq1 = new TestRequest();
		var rq2 = new TestRequest();

		var resultTask = Take(sut, 1, CT);

		sut.Add(src1, ct1.Token);
		sut.Add(src2, ct2.Token);

		ct1.Cancel();

		src1.Send(rq1);
		src2.Send(rq2);

		var result = await resultTask;

		result.Should().BeEquivalentTo(new[] { rq2 });
	}

	[TestMethod]
	public async Task When_Disposed_Then_EnumerationCompletes()
	{
		var sut = new CompositeRequestSource();

		var resultTask = Take(sut, 128, CT);

		sut.Dispose();

		resultTask.IsCompleted.Should().BeTrue();
	}

	[TestMethod]
	public async Task When_DisposedAndSendOnSource_Then_DoesNotThrow()
	{
		var sut = new CompositeRequestSource();
		var src1 = new RequestSource();
		var src2 = new RequestSource();
		var ct1 = CancellationTokenSource.CreateLinkedTokenSource(CT);
		var ct2 = CancellationTokenSource.CreateLinkedTokenSource(CT);
		var rq1 = new TestRequest();
		var rq2 = new TestRequest();

		var resultTask = Take(sut, 1, CT);

		sut.Add(src1, ct1.Token);
		sut.Add(src2, ct2.Token);

		sut.Dispose();

		src1.Send(rq1);
		src2.Send(rq2);

		resultTask.IsCompleted.Should().BeTrue();
	}

	private Task<List<IContextRequest>> Take(CompositeRequestSource sut, int count, CancellationToken ct)
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
