using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Commands;
using Uno.Extensions.Reactive.Testing;

namespace Uno.Extensions.Reactive.Tests.Commands;

[TestClass]
public class Given_CompositeCommand : FeedUITests
{
	[TestMethod]
	public void When_NoSub_Then_CannotExecute()
	{
		var sut = new CompositeAsyncCommand();

		sut.CanExecute(null).Should().BeFalse();
	}

	[TestMethod]
	public void When_AnySubCanExecute_Then_CanExecute()
	{
		var sub1 = new MyTestCommand(_ => true);
		var sub2 = new MyTestCommand(_ => false);
		var sut = new CompositeAsyncCommand(sub1, sub2);

		sut.CanExecute(null).Should().BeTrue();
	}

	[TestMethod]
	public void When_NoneSubCanExecute_Then_CanExecute()
	{
		var sub1 = new MyTestCommand(_ => false);
		var sub2 = new MyTestCommand(_ => false);
		var sut = new CompositeAsyncCommand(sub1, sub2);

		sut.CanExecute(null).Should().BeFalse();
	}

	[TestMethod]
	public void When_NoSub_Then_Execute()
	{
		var sut = new CompositeAsyncCommand();

		sut.Execute(null); // should not throw
	}

	[TestMethod]
	public void When_Execute_Then_AllSubThatCanExecuteAreExecuted()
	{
		var executed = new List<int>();
		var sub1 = new MyTestCommand(_ => true, _ => executed.Add(1));
		var sub2 = new MyTestCommand(_ => true, _ => executed.Add(2));
		var sub3 = new MyTestCommand(_ => false, _ => executed.Add(3));
		var sut = new CompositeAsyncCommand(sub1, sub2, sub3);

		sut.Execute(null);

		executed.Should().BeEquivalentTo(new[] { 1, 2 });
	}

	[TestMethod]
	public void When_NoSub_Then_IsNotExecuting()
	{
		var sut = new CompositeAsyncCommand();

		sut.IsExecuting.Should().BeFalse();
	}

	[TestMethod]
	public void When_AnySubIsExecuting_Then_IsExecuting()
	{
		var sub1 = new MyTestCommand() { IsExecuting = true };
		var sub2 = new MyTestCommand();
		var sut = new CompositeAsyncCommand(sub1, sub2);

		sut.IsExecuting.Should().BeTrue();
	}

	[TestMethod]
	public void When_NoneSubIsExecuting_Then_IsNotExecuting()
	{
		var sub1 = new MyTestCommand();
		var sub2 = new MyTestCommand();
		var sut = new CompositeAsyncCommand(sub1, sub2);

		sut.IsExecuting.Should().BeFalse();
	}

	[TestMethod]
	public void When_SubCanExecuteChanged_Then_CanExecuteChangedPropagated()
	{
		var sub1 = new MyTestCommand(_ => true, _ => throw new TestException());
		var sub2 = new MyTestCommand(_ => true, _ => throw new TestException());
		var sut = new CompositeAsyncCommand(sub1, sub2);
		var changed = 0;
		sut.CanExecuteChanged += (snd, e) => changed++;

		sub1.RaiseCanExecuteChanged();

		changed.Should().Be(1);
	}

	[TestMethod]
	public void When_SubPropertyChanged_Then_CanPropertyChangedPropagated()
	{
		var sub1 = new MyTestCommand(_ => true, _ => throw new TestException());
		var sub2 = new MyTestCommand(_ => true, _ => throw new TestException());
		var sut = new CompositeAsyncCommand(sub1, sub2);
		var changed = new List<string?>();
		sut.PropertyChanged += (snd, e) => changed.Add(e.PropertyName);

		sub1.RaisePropertyChanged("dummy_property");

		changed.Should().BeEquivalentTo(new[] { "dummy_property" });
	}

	private class MyTestCommand : IAsyncCommand
	{
		private readonly Predicate<object?> _canExecute;
		private readonly Action<object?> _execute;

		/// <inheritdoc />
		public event EventHandler? CanExecuteChanged;

		/// <inheritdoc />
		public event PropertyChangedEventHandler? PropertyChanged;


		public MyTestCommand(Predicate<object?>? canExecute = null, Action<object?>? execute = null)
		{
			_canExecute = canExecute ?? (_ => throw new TestException());
			_execute = execute ?? (_ => throw new TestException());
		}

		/// <inheritdoc />
		public bool IsExecuting { get; set; }

		/// <inheritdoc />
		public bool CanExecute(object? parameter)
			=> _canExecute(parameter);

		/// <inheritdoc />
		public void Execute(object? parameter)
			=> _execute(parameter);

		public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);

		public void RaisePropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
	}
}
