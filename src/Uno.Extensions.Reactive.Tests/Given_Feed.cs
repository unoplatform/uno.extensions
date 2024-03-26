using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Testing;

namespace Uno.Extensions.Reactive.Tests;

[TestClass]
public class Given_Feed : FeedTests
{
	// Those are integration tests that demo the basic public API and are not expected to deeply validate feed behavior.

	[TestMethod]
	public async Task When_GetAwaiter()
	{
		var sut = Feed.Async<int>(async ct => 42);
		var result = await sut;

		result.Should().Be(42);
	}

		[TestMethod]
		public async Task When_Async()
		{
			var sut = Feed.Async<int>(async ct => 42);
			var result = await sut.Data(CT);

		result.IsSome(out var items).Should().BeTrue();
		items.Should().Be(42);
	}

	[TestMethod]
	public async Task When_AsyncEnumerable()
	{
		async IAsyncEnumerable<int> GetSource()
		{
			yield return 41;
			yield return 42;
			yield return 43;
		}

		var expected = await GetSource().ToArrayAsync();
		var result = Feed<int>.AsyncEnumerable(GetSource).Record();

		await result.WaitForMessages(3, CT);

		result
			.Select(msg => msg.Current.Data.SomeOrDefault())
			.Should()
			.BeEquivalentTo(expected);
	}

	[TestMethod]
	public async Task When_Create()
	{
		async IAsyncEnumerable<Message<int>> GetSource([EnumeratorCancellation] CancellationToken ct)
		{
			var msg = Message<int>.Initial;

			yield return msg = msg.With().Data(41);
			yield return msg = msg.With().Data(42);
			yield return msg = msg.With().Data(43);
		}

		var expected = await GetSource(CT).Select(msg => msg.Current.Data.SomeOrDefault()).ToArrayAsync();
		var result = Feed.Create(GetSource).Record();

		await result.WaitForMessages(3, CT);

		result
			.Select(msg => msg.Current.Data.SomeOrDefault())
			.Should()
			.BeEquivalentTo(expected);
	}
}
