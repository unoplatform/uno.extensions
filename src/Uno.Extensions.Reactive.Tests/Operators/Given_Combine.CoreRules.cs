using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Operators;
using Uno.Extensions.Reactive.Testing;

namespace Uno.Extensions.Reactive.Tests.Operators;

[TestClass]
public partial class Given_Combine : FeedTests
{
	[TestMethod]
	public async Task When_CombineFeed_Then_CompilesToCoreRules()
	{
		var feed1 = Feed.Async(async _ => 42);
		var feed2 = Feed.Async(async _ => 43);
		await FeedCoreRules
			.WhenFeed(new CombineFeed<int, int>(feed1, feed2), feed1, feed2)
			.Then_CompilesToCoreRules(CT);
	}

	[TestMethod]
	public async Task When_CombineFeed3_Then_CompilesToCoreRules()
	{
		var feed1 = Feed.Async(async _ => 42);
		var feed2 = Feed.Async(async _ => 43);
		var feed3 = Feed.Async(async _ => 44);
		await FeedCoreRules
			.WhenFeed(new CombineFeed<int, int, int>(feed1, feed2, feed3), feed1, feed2, feed3)
			.Then_CompilesToCoreRules(CT);
	}
}
