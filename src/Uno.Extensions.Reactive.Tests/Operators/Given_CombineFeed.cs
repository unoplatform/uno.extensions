using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive;
using Uno.Extensions.Reactive.Testing;
using Uno.Extensions.Reactive.Tests.Sources;

namespace Uno.Reactive.Tests.Operators
{
	[TestClass]
	public class Given_CombineFeed : FeedTests
	{
		[TestMethod]
		public async Task When_Combine2()
		{
			var feed1 = new State<int>(Option<int>.Undefined());
			var feed2 = new State<int>(Option<int>.Undefined());

			var sut = Feed.Combine(feed1, feed2).Record();

			await feed1.Update(msg => msg.With().Data(42), CT);
			await feed2.Update(msg => msg.With().Data(43), CT);

			sut.Should().Be(r => r
				.Message(Changed.None, Data.Undefined, Error.No, Progress.Final)
				.Message(Changed.Data, (42, 43), Error.No, Progress.Final)
			);
		}
	}
}
