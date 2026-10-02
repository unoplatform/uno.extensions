using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Testing;

namespace Uno.Extensions.Reactive.Tests.Utils.Dispatching;

[TestClass]
public class Given_TestDispatcher
{
	[TestMethod]
	public async Task When_DisposeFromItsOwnThread_Then_DoesNotDeadlock()
	{
		var sut = new TestDispatcher();
		var disposed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

		sut.TryEnqueue(() =>
		{
			sut.Dispose();
			disposed.SetResult();
		});

		// The delay is only reached when Dispose deadlocks; the passing path completes immediately.
		var completed = await Task.WhenAny(disposed.Task, Task.Delay(TimeSpan.FromSeconds(5)));

		completed.Should().Be(disposed.Task, "disposing a dispatcher from its own thread must not wait for that thread to exit");
	}
}
