using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Operators;
using Uno.Extensions.Reactive.Testing;

namespace Uno.Extensions.Reactive.Tests.Operators;

[TestClass]
public partial class Given_ListFeedToFeedAdapter : FeedTests
{
	[TestMethod]
	public async Task When_ListFeedToFeedAdapter_Then_CompilesToCoreRules()
		=> await FeedCoreRules
			.Using(ListFeed.Async(async _ => new[] { new MyRecord(42) }.ToImmutableList() as IImmutableList<MyRecord>))
			.WhenFeed(src => new ListFeedToFeedAdapter<MyRecord>(src))
			.Then_CompilesToCoreRules(CT);

	private record MyRecord(int Value);
}
