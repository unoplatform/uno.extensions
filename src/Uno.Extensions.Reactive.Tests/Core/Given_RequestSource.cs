using System;
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

		var resultTask = sut.Take(1).ToListAsync(CT);

		sut.Send(rq);

		var result = await resultTask;

		result.Should().BeEquivalentTo(new[] { rq });
	}

	[TestMethod]
	public async Task When_Disposed_Then_EnumerationCompletes()
	{
		var sut = new RequestSource();

		var resultTask = sut.ToListAsync(CT);

		sut.Dispose();

		resultTask.IsCompleted.Should().BeTrue();
	}

	[TestMethod]
	[ExpectedException(typeof(InvalidOperationException))]
	public async Task When_DisposedAndSend_Then_Throws()
	{
		var sut = new CompositeRequestSource();
		var rq = new TestRequest();

		sut.Dispose();
		sut.Send(rq);
	}
}
