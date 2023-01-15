using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions.Execution;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Operators;
using Uno.Extensions.Reactive.Testing;

namespace Uno.Extensions.Reactive.Tests.Operators;

partial class Given_ListFeedSelection : FeedTests
{
	[TestMethod]
	public async Task When_ListFeedSelection_Then_CompliesToCoreRules()
	{
		var state = State.Value(this, () => new MyRecord(-42));
		var sut = FeedCoreRules
			.Using(ListFeed.Async(async _ => ImmutableList.Create(new MyRecord(42))))
			.WhenListFeed(src => ListFeedSelection<MyRecord>.Create(src, state, "sut"))
			.Then_CompilesToCoreRules(CT);
	}

	private record MyRecord(int Value);
}
