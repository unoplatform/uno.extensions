using System;
using System.Linq;
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
		var result = sut.ToListAsync(CT).IsCompleted;

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
}
