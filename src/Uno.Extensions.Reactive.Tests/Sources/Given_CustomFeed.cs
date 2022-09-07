using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive;
using Uno.Extensions.Reactive.Testing;

namespace Uno.Extensions.Reactive.Tests.Sources
{
	[TestClass]
	public class Given_CustomFeed : FeedTests
	{
		[TestMethod]
		public async Task When_CreateFeed()
		{
			async IAsyncEnumerable<Message<int>> GetSource([EnumeratorCancellation] CancellationToken ct)
			{
				var current = Message<int>.Initial;
				yield return current = current.With().Data(42);
				yield return current = current.With().Data(43);
				yield return current = current.With().Error(new TestException());
				yield return current = current.With().Data(44).Error(null).IsTransient(true);
			}

			var sut = Feed.Create(GetSource).Record();

			await sut.Should().BeAsync(r => r
				.Message(Changed.Data, 42, Error.No, Progress.Final)
				.Message(Changed.Data, 43, Error.No, Progress.Final)
				.Message(Changed.Error, 43, typeof(TestException), Progress.Final)
				.Message(Changed.Data & Changed.Error & Changed.Progress, 44, Error.No, Progress.Transient)
			);
		}

		[TestMethod]
		public async Task When_CreateFeedAndSendInvalidMessage_Then_Fails()
		{
			async IAsyncEnumerable<Message<int>> GetSource([EnumeratorCancellation] CancellationToken ct)
			{
				yield return Message<int>.Initial.With().Data(42).IsTransient(true);
				yield return Message<int>.Initial.With().Data(43);
			}

			var sut = Feed.Create(GetSource).Record();

			await sut.Should().BeAsync(r => r
				.Message(Changed.Data & Changed.Progress, 42, Error.No, Progress.Transient)
				.Message(Changed.Error & Changed.Progress, 42, typeof(InvalidOperationException), Progress.Final)
			);
		}

	}
}
