using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Operators;
using Uno.Extensions.Reactive.Testing;

namespace Uno.Extensions.Reactive.Tests.Operators;

public partial class Given_FeedToListFeedAdapter : FeedTests
{
	[TestMethod]
	public async Task When_FeedToListFeedAdapter_Then_CompilesToCoreRules()
		=> await FeedCoreRules
			.Using(Feed.Async(async _ => new[] { new MyRecord(42) }.ToImmutableList() as IImmutableList<MyRecord>))
			.WhenListFeed(src => new FeedToListFeedAdapter<MyRecord>(src))
			.Then_CompilesToCoreRules(CT);

	private record MyRecord(int Value);
}
