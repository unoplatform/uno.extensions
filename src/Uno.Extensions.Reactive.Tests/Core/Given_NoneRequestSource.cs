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
public class Given_NoneRequestSource : FeedTests
{
	[TestMethod]
	public void When_Enumerate_Then_CompletesSync()
	{
		var sut = new NoneRequestSource();
		var result = ToList(sut, CT).IsCompleted;

		result.Should().BeTrue();
	}

	[TestMethod]
	public void When_Dispose()
	{
		new NoneRequestSource().Dispose();
	}

	[TestMethod]
	public void When_Send()
	{
		new NoneRequestSource().Send(new TestRequest());
	}

	private Task<List<IContextRequest>> ToList(NoneRequestSource sut, CancellationToken ct)
	{
		var result = new List<IContextRequest>();
		var tcs = new TaskCompletionSource<List<IContextRequest>>();
		sut.RequestRaised += OnRequest;
		ct.Register(Complete);

		return tcs.Task;

		void OnRequest(object? sender, IContextRequest req)
		{
			result.Add(req);
			if (req is End)
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
