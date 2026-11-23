using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Core;

namespace Uno.Extensions.Reactive.Tests.Core;

[TestClass]
public class Given_MockingReadsHold
{
	[TestMethod]
	public void When_Released_Then_HeldContinuationsRunOnce()
	{
		var sut = new MockingReadsHold();
		var runs = 0;
		sut.OnCompleted(() => runs++);
		sut.OnCompleted(() => runs++);

		sut.IsCompleted.Should().BeFalse();
		sut.Release();
		sut.Release();

		sut.IsCompleted.Should().BeTrue();
		runs.Should().Be(2);
	}

	[TestMethod]
	public void When_AlreadyReleased_Then_AContinuationRunsAtOnce()
	{
		var sut = new MockingReadsHold();
		sut.Release();
		var ran = false;

		sut.OnCompleted(() => ran = true);

		ran.Should().BeTrue();
	}

	[TestMethod]
	public void When_AContinuationThrows_Then_TheOthersStillRunAndTheFailureIsReported()
	{
		var sut = new MockingReadsHold();
		var ran = new List<int>();
		sut.OnCompleted(() => ran.Add(1));
		sut.OnCompleted(() => throw new InvalidOperationException("second"));
		sut.OnCompleted(() => ran.Add(3));

		var release = sut.Release;

		release.Should().Throw<AggregateException>().WithInnerException<InvalidOperationException>();
		ran.Should().Equal(1, 3);
	}
}
