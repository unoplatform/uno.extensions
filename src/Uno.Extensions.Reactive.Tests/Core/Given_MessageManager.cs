using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Testing;

namespace Uno.Extensions.Reactive.Tests.Core;

[TestClass]
public class Given_MessageManager : FeedTests
{
	[TestMethod]
	public void When_LocalSetAxisWithChanges_Then_ChangesForwarded()
	{
		(object value, TestChangeSet changes) local = (new(), new());

		var axis = new MessageAxis<object>("testAxis", _ => throw new InvalidOperationException("Should not be used"));
		var sut = new MessageManager<object, object>();

		sut.Update(current => current.With().Set(axis, axis.ToMessageValue(local.value), local.changes), CT);

		sut.Current.Current.Get(axis).Should().Be(local.value);
		sut.Current.Changes.Contains(axis, out var actual).Should().BeTrue();
		(actual as object).Should().Be(local.changes);
	}

	[TestMethod]
	public void When_ParentSetAxisWithChanges_Then_ChangesForwarded()
	{
		(object value, TestChangeSet changes) parent = (new(), new());

		var axis = new MessageAxis<object>("testAxis", _ => throw new InvalidOperationException("Should not be used"));
		var parentMessage = Message<object>.Initial.With().Set(axis, axis.ToMessageValue(parent.value), parent.changes);
		var sut = new MessageManager<object, object>();

		sut.Update(current => current.With(parentMessage), CT);

		sut.Current.Current.Get(axis).Should().Be(parent.value);
		sut.Current.Changes.Contains(axis, out var actual).Should().BeTrue();
		(actual as object).Should().Be(parent.changes);
	}

	[TestMethod]
	public void When_ParentAndLocalSetAxisWithChanges_Then_ChangesAreNotForwarded()
	{
		// Remarks: We don't have yet a way to merge ChangeSet, so this test is only safety guard to make sure that we don't forward invalid an invalid ChangeSet
		// (which contains either only the changes of the parent or the local changes, ignoring the fact that we did merged the value)

		(object value, TestChangeSet changes) parent = (new(), new());
		(object value, TestChangeSet changes) local = (new(), new());

		var axis = new MessageAxis<object>("testAxis", _ => 42);
		var parentMessage = Message<object>.Initial.With().Set(axis, axis.ToMessageValue(parent.value), parent.changes);
		var sut = new MessageManager<object, object>();

		sut.Update(current => current.With(parentMessage).Set(axis, axis.ToMessageValue(local.value), local.changes), CT);

		sut.Current.Current.Get(axis).Should().Be(42);
		sut.Current.Changes.Contains(axis, out var actual).Should().BeTrue();
		(actual as object).Should().BeNull();
	}

	[TestMethod]
	public void When_ParentAndThenLocalSetAxisWithChanges_Then_ChangesAreNotForwarded()
	{
		// Remarks: We don't have yet a way to merge ChangeSet, so this test is only safety guard to make sure that we don't forward invalid an invalid ChangeSet
		// (which contains either only the changes of the parent or the local changes, ignoring the fact that we did merged the value)

		(object value, TestChangeSet changes) parent = (new(), new());
		(object value, TestChangeSet changes) local = (new(), new());

		var axis = new MessageAxis<object>("testAxis", _ => 42);
		var parentMessage = Message<object>.Initial.With().Set(axis, axis.ToMessageValue(parent.value), parent.changes);
		var sut = new MessageManager<object, object>();

		sut.Update(current => current.With(parentMessage), CT);
		sut.Update(current => current.With().Set(axis, axis.ToMessageValue(local.value), local.changes), CT);

		sut.Current.Current.Get(axis).Should().Be(42);
		sut.Current.Changes.Contains(axis, out var actual).Should().BeTrue();
		(actual as object).Should().BeNull();
	}

	[TestMethod]
	public void When_LocalTheParentSetAxisWithChanges_Then_ChangesAreNotForwarded()
	{
		// Remarks: We don't have yet a way to merge ChangeSet, so this test is only safety guard to make sure that we don't forward invalid an invalid ChangeSet
		// (which contains either only the changes of the parent or the local changes, ignoring the fact that we did merged the value)

		(object value, TestChangeSet changes) parent = (new(), new());
		(object value, TestChangeSet changes) local = (new(), new());

		var axis = new MessageAxis<object>("testAxis", _ => 42);
		var parentMessage = Message<object>.Initial.With().Set(axis, axis.ToMessageValue(parent.value), parent.changes);
		var sut = new MessageManager<object, object>();

		sut.Update(current => current.With().Set(axis, axis.ToMessageValue(local.value), local.changes), CT);
		sut.Update(current => current.With(parentMessage), CT);

		sut.Current.Current.Get(axis).Should().Be(42);
		sut.Current.Changes.Contains(axis, out var actual).Should().BeTrue();
		(actual as object).Should().BeNull();
	}

	private record TestChangeSet(params IChange[] Changes) : IChangeSet
	{
		/// <inheritdoc />
		public IEnumerator<IChange> GetEnumerator()
			=> Changes.AsEnumerable().GetEnumerator();

		/// <inheritdoc />
		IEnumerator IEnumerable.GetEnumerator()
			=> GetEnumerator();
	}
}
