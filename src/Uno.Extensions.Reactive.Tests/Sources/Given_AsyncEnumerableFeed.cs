#nullable enable

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Sources;
using Uno.Extensions.Reactive.Testing;
using Uno.Extensions.Reactive.Core;

namespace Uno.Extensions.Reactive.Tests.Sources;

[TestClass]
public class Given_AsyncEnumerableFeed : FeedTests
{
	[TestMethod]
	public async Task When_ProviderReturnsValuesSync_Then_GetSome()
	{
		async IAsyncEnumerable<Option<int>> GetSource([EnumeratorCancellation] CancellationToken ct = default)
		{
			yield return 42;
			yield return 43;
			yield return 43;
			yield return default;
			yield return Option<int>.Undefined();
			yield return 44;
		}

		var sut = new AsyncEnumerableFeed<int>(() => GetSource());
		using var result = sut.Record();

		await result.Should().BeAsync(r => r
			.Message(Changed.Data, 42, Error.No, Progress.Final)
			.Message(Changed.Data, 43, Error.No, Progress.Final)
			.Message(Changed.Data, Data.None, Error.No, Progress.Final)
			.Message(Changed.Data, Data.Undefined, Error.No, Progress.Final)
			.Message(Changed.Data, 44, Error.No, Progress.Final)
		);
	}

	[TestMethod]
	public async Task When_ProviderReturnsValuesAsync_Then_GetSome()
	{
		async IAsyncEnumerable<Option<int>> GetSource([EnumeratorCancellation] CancellationToken ct = default)
		{
			await Task.Delay(10, ct);
			yield return 42;
			await Task.Delay(10, ct);
			yield return 43;
			await Task.Delay(10, ct);
			yield return 43;
			await Task.Delay(10, ct);
			yield return default;
			await Task.Delay(10, ct);
			yield return Option<int>.Undefined();
			await Task.Delay(10, ct);
			yield return 44;
		}

		var sut = new AsyncEnumerableFeed<int>(() => GetSource());
		using var result = sut.Record();

		await result.Should().BeAsync(r => r
			.Message(Changed.Data, 42, Error.No, Progress.Final)
			.Message(Changed.Data, 43, Error.No, Progress.Final)
			.Message(Changed.Data, Data.None, Error.No, Progress.Final)
			.Message(Changed.Data, Data.Undefined, Error.No, Progress.Final)
			.Message(Changed.Data, 44, Error.No, Progress.Final)
		);
	}

	[TestMethod]
	public async Task When_ProviderThrowsSync_Then_GetError()
	{
		var sut = new AsyncEnumerableFeed<int>(factory: () => throw new TestException());
		using var result = sut.Record();

		await result.Should().BeAsync(r => r
			.Message(Changed.Error, Data.Undefined, typeof(TestException), Progress.Final)
		);
	}

	[TestMethod]
	public async Task When_ProviderThrowsAsync_Then_GetError()
	{
		async IAsyncEnumerable<Option<int>> GetSource([EnumeratorCancellation] CancellationToken ct = default)
		{
			await Task.Delay(10, ct);
			throw new TestException();
#pragma warning disable CS0162
			yield return 42;
#pragma warning restore CS0162
		}

		var sut = new AsyncEnumerableFeed<int>(() => GetSource());
		using var result = sut.Record();

		await result.Should().BeAsync(r => r
			.Message(Changed.Error, Data.Undefined, typeof(TestException), Progress.Final)
		);
	}

	[TestMethod]
	public async Task When_ProviderReturnSomeThenThrows_Then_KeepPreviousData()
	{
		async IAsyncEnumerable<Option<int>> GetSource([EnumeratorCancellation] CancellationToken ct = default)
		{
			yield return 42;
			throw new TestException();
		}

		var sut = new AsyncEnumerableFeed<int>(() => GetSource());
		using var result = sut.Record();

		await result.Should().BeAsync(r => r
			.Message(Changed.Data, 42, Error.No, Progress.Final)
			.Message(Changed.Error, 42, typeof(TestException), Progress.Final)
		);
	}

	[TestMethod]
	public async Task When_NoContext_Then_NoCaching()
	{
		Context.ResignCurrent();

		var invokeCount = 0;
		async IAsyncEnumerable<Option<int>> GetSource([EnumeratorCancellation] CancellationToken ct = default)
		{
			invokeCount++;
			yield return 42;
		}

		var sut = new AsyncEnumerableFeed<int>(() => GetSource());

		await sut;
		await sut;

		invokeCount.Should().Be(2);
	}

	[TestMethod]
	public async Task When_InvokeProvider_Then_ContextSet()
	{
		Context.ResignCurrent(); // We prefer to explicitly give it in GetSource to avoid any uncontrolled flowing

		async IAsyncEnumerable<Option<int>> GetSource([EnumeratorCancellation] CancellationToken ct = default)
		{
			SourceContext.Current.Should().Be(Context.SourceContext);
			yield return 42;
		}

		var sut = new AsyncEnumerableFeed<int>(() => GetSource());
		using var result = sut.Record(Context.SourceContext);

		await result.Should().BeAsync(r => r
			.Message(Error.No)
		);
	}
}
