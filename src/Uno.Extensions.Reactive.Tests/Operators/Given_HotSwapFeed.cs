using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Operators;
using Uno.Extensions.Reactive.Testing;

namespace Uno.Extensions.Reactive.Tests.Operators;

[TestClass]
public class Given_HotSwapFeed : FeedTests
{
	private static readonly TimeSpan Patience = TimeSpan.FromSeconds(2);

	[TestMethod]
	public async Task When_SetWhileAReadIsPending_Then_TheReadFollowsTheNewFeed()
	{
		var (sut, reader) = await StartReadingOriginal();

		var pending = reader.MoveNextAsync().AsTask();
		sut.Set(Feed.Async(async ct => 2));

		(await Task.WhenAny(pending, Task.Delay(Patience))).Should().BeSameAs(pending, "a pending read is woken by the swap");
		(await pending).Should().BeTrue();
		(reader.Current.Current.Data.SomeOrDefault(-1) == 2 || await ReadUntil(reader, 2)).Should().BeTrue();
	}

	[TestMethod]
	public async Task When_SetBetweenTwoReads_Then_TheNextReadFollowsTheNewFeed()
	{
		var (sut, reader) = await StartReadingOriginal();

		sut.Set(Feed.Async(async ct => 2));

		(await ReadUntil(reader, 2)).Should().BeTrue("a swap made while no read is pending must not be lost");
	}

	private async Task<(HotSwapFeed<int> sut, IAsyncEnumerator<Message<int>> reader)> StartReadingOriginal()
	{
		var sut = new HotSwapFeed<int>(Feed.Async(async ct => 1));
		var reader = sut.GetSource(Context.SourceContext, CT).GetAsyncEnumerator(CT);
		(await ReadUntil(reader, 1)).Should().BeTrue("the original feed produces 1");

		return (sut, reader);
	}

	// Gives up after a while instead of hanging: a lost swap leaves the read waiting forever.
	private static async Task<bool> ReadUntil(IAsyncEnumerator<Message<int>> reader, int expected)
	{
		var deadline = Task.Delay(Patience);
		while (true)
		{
			var move = reader.MoveNextAsync().AsTask();
			if (await Task.WhenAny(move, deadline) != move || !await move)
			{
				return false;
			}

			if (reader.Current.Current.Data.SomeOrDefault(-1) == expected)
			{
				return true;
			}
		}
	}
}
