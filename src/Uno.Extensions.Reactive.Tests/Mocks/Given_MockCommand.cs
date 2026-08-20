using System;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Mocks;

namespace Uno.Extensions.Reactive.Tests.Mocks;

[TestClass]
public class Given_MockCommand
{
	[TestMethod]
	public void When_Idle_Then_CanExecuteIsTrue_And_IsExecutingIsFalse()
	{
		var sut = MockCommand.Idle();

		sut.CanExecute(null).Should().BeTrue();
		sut.IsExecuting.Should().BeFalse();
		sut.Invoking(c => c.Execute(null)).Should().NotThrow();
	}

	[TestMethod]
	public void When_Disabled_Then_CanExecuteIsFalse()
	{
		var sut = MockCommand.Disabled();

		sut.CanExecute(null).Should().BeFalse();
		sut.IsExecuting.Should().BeFalse();
	}

	[TestMethod]
	public void When_Executing_Then_IsExecutingIsTrue_And_CanExecuteIsFalse()
	{
		var sut = MockCommand.Executing();

		sut.IsExecuting.Should().BeTrue();
		sut.CanExecute(null).Should().BeFalse();
	}

	[TestMethod]
	public void When_Callback_Then_ExecuteInvokesCallbackWithParameter()
	{
		object? received = null;
		var sut = MockCommand.Callback(p => received = p);

		sut.CanExecute(null).Should().BeTrue();
		sut.Execute("some-parameter");

		received.Should().Be("some-parameter");
	}

	[TestMethod]
	public void When_CallbackNotExecutable_Then_CanExecuteIsFalse_And_ExecuteDoesNotInvoke()
	{
		var invoked = false;
		var sut = MockCommand.Callback(_ => invoked = true, canExecute: false);

		sut.CanExecute(null).Should().BeFalse();

		// Like the real AsyncCommand, a programmatic Execute on a non-executable command is a no-op.
		sut.Execute(null);
		invoked.Should().BeFalse();
	}
}
