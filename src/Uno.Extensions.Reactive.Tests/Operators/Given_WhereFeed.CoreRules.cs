using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Operators;
using Uno.Extensions.Reactive.Testing;

namespace Uno.Extensions.Reactive.Tests.Operators;

[TestClass]
public partial class Given_WhereFeed : FeedTests
{
	[TestMethod]
	public async Task When_WhereFeed_Then_CompilesToCoreRules()
		=> await FeedCoreRules
			.Using(Feed.Async(async _ => new MyRecord(42)))
			.WhenFeed(src => new WhereFeed<MyRecord>(src, (MyRecord _) => true))
			.Then_CompilesToCoreRules(CT);

	private record MyRecord(int Value);
}
