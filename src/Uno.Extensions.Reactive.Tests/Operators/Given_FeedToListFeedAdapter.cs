using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Equality;
using Uno.Extensions.Reactive.Operators;
using Uno.Extensions.Reactive.Testing;

namespace Uno.Extensions.Reactive.Tests.Operators;

[TestClass]
public partial class Given_FeedToListFeedAdapter : FeedTests
{
	[TestMethod]
	public async Task When_Null_Then_None()
	{
		var source = Feed.Async(async ct => default(IImmutableList<int>)!);
		var sut = source.AsListFeed();
		var result = sut.Record();

		await result.Should().BeAsync(r => r
			.Message(Changed.Data, Data.None, Error.No, Progress.Final)
		);
	}

	[TestMethod]
	public async Task When_Empty_Then_None()
	{
		var source = Feed.Async(async ct => ImmutableList<int>.Empty as IImmutableList<int>);
		var sut = source.AsListFeed();
		var result = sut.Record();

		await result.Should().BeAsync(r => r
			.Message(Changed.Data, Data.None, Error.No, Progress.Final)
		);
	}

	[TestMethod]
	public async Task When_KeyEquatableNoComparerAndUpdate_Then_TrackItemsUsingKeyEquality()
	{
		var original = new MyKeyedRecord[] { new(1, 1), new(2, 1), new(3, 1), new(4, 1) };
		var updated = new MyKeyedRecord[] { new(1, 1), new(2, 2), new(4, 1), new(5, 1) };

		async IAsyncEnumerable<IImmutableList<MyKeyedRecord>> GetSource([EnumeratorCancellation] CancellationToken ct = default)
		{
			await Task.Yield(); // Make sure to run async, so listener will receive all messages.

			yield return original.ToImmutableList();
			yield return updated.ToImmutableList();
		}

		var source = Feed.AsyncEnumerable(GetSource);
		var sut = new FeedToListFeedAdapter<MyKeyedRecord>(source);
		var result = sut.Record();

		await result.Should().BeAsync(r => r
			.Message(m => m
				.Current(Items.Some(original), Error.No, Progress.Final)
				.Changed(Items.Reset(original)))
			.Message(m => m
				.Current(Items.Some(updated), Error.No, Progress.Final)
				.Changed(Items.Replace(1, new MyKeyedRecord(2,1), new MyKeyedRecord(2,2))
					& Items.Remove(2, new MyKeyedRecord(3,1))
					& Items.Add(3, new MyKeyedRecord(5,1))))
		);
	}

	[TestMethod]
	public async Task When_NotKeyEquatableNoComparerAndUpdate_Then_TrackItemsUsingKeyEquality()
	{
		var original = new MyNotKeyedRecord[] { new(1, 1), new(2, 1), new(3, 1), new(4, 1) };
		var updated = new MyNotKeyedRecord[] { new(1, 1), new(2, 2), new(4, 1), new(5, 1) };

		async IAsyncEnumerable<IImmutableList<MyNotKeyedRecord>> GetSource([EnumeratorCancellation] CancellationToken ct = default)
		{
			await Task.Yield(); // Make sure to run async, so listener will receive all messages.

			yield return original.ToImmutableList();
			yield return updated.ToImmutableList();
		}

		var source = Feed.AsyncEnumerable(GetSource);
		var sut = new FeedToListFeedAdapter<MyNotKeyedRecord>(source);
		var result = sut.Record();

		await result.Should().BeAsync(r => r
			.Message(m => m
				.Current(Items.Some(original), Error.No, Progress.Final)
				.Changed(Items.Reset(original)))
			.Message(m => m
				.Current(Items.Some(updated), Error.No, Progress.Final)
				.Changed(Items.Remove(1, new MyNotKeyedRecord(2, 1), new MyNotKeyedRecord(3, 1))
					& Items.Add(1, new MyNotKeyedRecord(2, 2))
					& Items.Add(3, new MyNotKeyedRecord(5, 1))))
		);
	}

	[TestMethod]
	public async Task When_ClassNoComparerAndUpdate_Then_TrackItemsUsingKeyEquality()
	{
		var original = new MyNotKeyedClass[] { new(1, 1), new(2, 1), new(3, 1), new(4, 1) };
		var updated = new MyNotKeyedClass[] { new(1, 1), new(2, 2), new(4, 1), new(5, 1) };

		async IAsyncEnumerable<IImmutableList<MyNotKeyedClass>> GetSource([EnumeratorCancellation] CancellationToken ct = default)
		{
			await Task.Yield(); // Make sure to run async, so listener will receive all messages.

			yield return original.ToImmutableList();
			yield return updated.ToImmutableList();
		}

		var source = Feed.AsyncEnumerable(GetSource);
		var sut = new FeedToListFeedAdapter<MyNotKeyedClass>(source);
		var result = sut.Record();

		await result.Should().BeAsync(r => r
			.Message(m => m
				.Current(Items.Some(original), Error.No, Progress.Final)
				.Changed(Items.Reset(original)))
			.Message(m => m
				.Current(Items.Some(updated), Error.No, Progress.Final)
				.Changed(Items.Remove(0, original)
					& Items.Add(0, updated)))
		);
	}

	public partial record MyKeyedRecord(int Id, int Version);

	[ImplicitKeys(IsEnabled = false)]
	public partial record MyNotKeyedRecord(int Id, int Version);

	public partial class MyNotKeyedClass
	{
		public MyNotKeyedClass(int id, int version)
		{
			Id = id;
			Version = version;
		}

		public int Id { get; init; }
		public int Version { get; init; }
	}
}
