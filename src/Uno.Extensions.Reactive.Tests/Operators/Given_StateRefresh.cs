using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Core;
using Uno.Extensions.Reactive.Messaging;
using Uno.Extensions.Reactive.Testing;

namespace Uno.Extensions.Reactive.Tests.Operators;

[TestClass]
public class Given_StateRefresh : FeedTests
{
	[TestMethod]
	public async Task When_RequestRefreshRefreshableFeed_Then_ReturnsTrueAndGetUpdatedValue()
	{
		var src = Feed.Async(async _ => 42);
		var sut = new StateImpl<int>(Context, src);
		var result = sut.Record();

		await result.Should().BeAsync(m => m
			.Message(42, Error.No, Progress.Final));

		sut.RequestRefresh().Should().BeTrue();

		await result.Should().BeAsync(m => m
			.Message(42, Error.No, Progress.Final)
			.Message(42, Error.No, Progress.Final, Refreshed.Is(1)));
	}

	[TestMethod]
	public async Task When_RequestRefreshNonRefreshableFeed_Then_ReturnsFalseAndNoUpdate()
	{
		static async IAsyncEnumerable<Message<int>> GetMessages([EnumeratorCancellation] CancellationToken ct)
		{
			yield return Message<int>.Initial.With().Data(42);
		}

		var src = Feed.Create(GetMessages);
		var sut = new StateImpl<int>(Context, src);
		var result = sut.Record();

		await result.Should().BeAsync(m => m
			.Message(42, Error.No, Progress.Final));

		sut.RequestRefresh().Should().BeFalse();

		await result.Should().BeAsync(m => m
			.Message(42, Error.No, Progress.Final));
	}

	[TestMethod]
	public async Task When_TryRefreshAsyncRefreshableFeed_Then_ReturnsTrueAndTaskCompleteWhenGetValue()
	{
		var value = new TaskCompletionSource<int>(CT);
		var src = Feed.Async(async _ => await value.Task.ConfigureAwait(false));
		var sut = new StateImpl<int>(Context, src);
		var result = sut.Record();

		await result.Should().BeAsync(m => m
			.Message(Data.Undefined, Error.No, Progress.Transient)
		);

		value.SetResult(42);

		await result.Should().BeAsync(m => m
			.Message(Data.Undefined, Error.No, Progress.Transient)
			.Message(42, Error.No, Progress.Final)
		);

		value = new TaskCompletionSource<int>(CT);
		var request = sut.TryRefreshAsync();

		await result.Should().BeAsync(m => m
			.Message(Data.Undefined, Error.No, Progress.Transient)
			.Message(42, Error.No, Progress.Final)
			.Message(42, Error.No, Progress.Transient)
		);
		request.IsCompleted.Should().BeFalse();

		value.SetResult(43);

		await result.Should().BeAsync(m => m
			.Message(Data.Undefined, Error.No, Progress.Transient)
			.Message(42, Error.No, Progress.Final)
			.Message(42, Error.No, Progress.Transient)
			.Message(43, Error.No, Progress.Final)
		);
		(await request).Should().BeTrue(); // Check async as completion is ran from another thread.
	}

	[TestMethod]
	public async Task When_TryRefreshAsyncNonRefreshableFeed_Then_ReturnsFalseSyncAndNoUpdate()
	{
		static async IAsyncEnumerable<Message<int>> GetMessages([EnumeratorCancellation] CancellationToken ct)
		{
			yield return Message<int>.Initial.With().Data(42);
		}

		var src = Feed.Create(GetMessages);
		var sut = new StateImpl<int>(Context, src);
		var result = sut.Record();

		await result.Should().BeAsync(m => m
			.Message(42, Error.No, Progress.Final)
		);

		var request = sut.TryRefreshAsync();
		request.IsCompleted.Should().BeTrue();
		request.Result.Should().BeFalse();

		await result.Should().BeAsync(m => m
			.Message(42, Error.No, Progress.Final));
	}
}
