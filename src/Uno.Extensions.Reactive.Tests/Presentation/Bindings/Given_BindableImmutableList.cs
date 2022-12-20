using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Bindings;
using Uno.Extensions.Reactive.Testing;
using Uno.Extensions.Reactive.Tests.Generator;

namespace Uno.Extensions.Reactive.Tests.Presentation.Bindings;

[TestClass]
public partial class Given_BindableImmutableList : FeedTests
{
	[TestMethod]
	public void When_Init()
	{
		var prop = new BindablePropertyInfo<IImmutableList<int>>(
			new TestBindable(),
			"sut",
			(default!, changed => changed(ImmutableList.Create(0, 1, 2, 3, 4, 5, 6, 7, 8, 9))),
			async (_, __, ct) => { });

		var createdItemBindable = 0;
		var sut = new BindableImmutableList<int, Bindable<int>>(prop, itemProp =>
		{
			createdItemBindable++;
			return new Bindable<int>(itemProp);
		});

		createdItemBindable.Should().Be(10);
		sut.Count.Should().Be(10);
	}

	private class TestBindable : IBindable
	{
	}
}
