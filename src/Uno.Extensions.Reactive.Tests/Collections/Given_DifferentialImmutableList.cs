using System;
using System.Collections;
using System.Collections.Immutable;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Collections.Facades.Differential;

namespace Uno.Extensions.Reactive.Tests.Collections;

[TestClass]
public class Given_DifferentialImmutableList
{
	[TestMethod]
	public void When_IndexOf()
		=> GetSut().IndexOf(5).Should().Be(5);

	[TestMethod]
	public void When_IndexOfMissing()
		=> GetSut().IndexOf(42).Should().Be(-1);

	[TestMethod]
	[Ignore("Not implemented yet")]
	public void When_LastIndexOf()
		=> GetSut().LastIndexOf(5).Should().Be(5);

	[TestMethod]
	public void When_GetByIndex()
		=> GetSut()[5].Should().Be(5);

	[TestMethod]
	public void When_Enumerate()
		=> GetSut().Should().BeEquivalentTo(Enumerable.Range(0, 10));

	[TestMethod]
	public void When_CopyTo()
	{
		var sut = GetSut();
		var target = new int[sut.Count + 2];
		target[0] = -1;
		target[^1] = -1;

		sut.CopyTo(target, 1);

		target[0].Should().Be(-1);
		target[^1].Should().Be(-1);
		target.Skip(1).Take(sut.Count).Should().BeEquivalentTo(sut);
	}

	[TestMethod]
	public void When_Add()
		=> Validate(sut => sut.Add(42));

	[TestMethod]
	public void When_AddRange()
		=> Validate(sut => sut.AddRange(new[] { 42, 43, 44 }));

	[TestMethod]
	public void When_Insert()
		=> Validate(sut => sut.Insert(5, 42));

	[TestMethod]
	public void When_InsertRange()
		=> Validate(sut => sut.InsertRange(5, new[] { 42, 43, 44 }));

	[TestMethod]
	public void When_Remove()
		=> Validate(sut => sut.Remove(5));

	[TestMethod]
	public void When_RemoveAll()
		=> Validate(sut => sut.RemoveAll(i => i % 2 == 0));

	[TestMethod]
	[Ignore("Not implemented yet")]
	public void When_RemoveRangeOfItems()
		=> Validate(sut => sut.RemoveRange(Enumerable.Range(3, 3)));

	[TestMethod]
	public void When_RemoveRangeByIndex()
		=> Validate(sut => sut.RemoveRange(3, 3));

	[TestMethod]
	public void When_Clear()
		=> Validate(sut => sut.Clear());

	[TestMethod]
	public void When_SetItem()
		=> Validate(sut => sut.SetItem(3, 42));

	[TestMethod]
	public void When_AsList_IndexOf()
		=> (GetSut() as IList).IndexOf(5).Should().Be(5);

	[TestMethod]
	public void When_AsList_IndexOfMissing()
		=> (GetSut() as IList).IndexOf(42).Should().Be(-1);

	[TestMethod]
	public void When_AsList_Contains()
		=> (GetSut() as IList).Contains(5).Should().BeTrue();

	[TestMethod]
	public void When_AsList_ContainsMissing()
		=> (GetSut() as IList).Contains(42).Should().BeFalse();

	[TestMethod]
	public void When_AsList_Add()
		=> Assert.ThrowsExactly<InvalidOperationException>(() => (GetSut() as IList).Add(42));

	[TestMethod]
	public void When_AsList_Insert()
		=> Assert.ThrowsExactly<InvalidOperationException>(() => (GetSut() as IList).Insert(5, 42));

	[TestMethod]
	public void When_AsList_Remove()
		=> Assert.ThrowsExactly<InvalidOperationException>(() => (GetSut() as IList).Remove(5));

	[TestMethod]
	public void When_AsList_RemoveAt()
		=> Assert.ThrowsExactly<InvalidOperationException>(() => (GetSut() as IList).RemoveAt(5));

	[TestMethod]
	public void When_AsList_Clear()
		=> Assert.ThrowsExactly<InvalidOperationException>(() => (GetSut() as IList).Clear());

	private DifferentialImmutableList<int> GetSut()
		=> new(Enumerable.Range(0, 10).ToImmutableList());

	private void Validate(Func<IImmutableList<int>, IImmutableList<int>> act)
	{
		var items = Enumerable.Range(0, 10).ToImmutableList();
		var sut = new DifferentialImmutableList<int>(items);

		var expected = act(items);
		var actual = act(sut);

		sut.Should().BeEquivalentTo(items, because: "the source collection should not have been modified");
		actual.Should().BeEquivalentTo(expected, because: "the modification should have the same behavior as a standard ImmutableList");
	}
}
