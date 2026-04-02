using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Sources;
using Uno.Extensions.Reactive.Testing;

namespace Uno.Extensions.Reactive.Tests.Sources;

[TestClass]
public class Given_ValueFeed : FeedTests
{
	#region Single value emission
	[TestMethod]
	public async Task When_SomeValue_Then_ProducesOneMessageWithData()
	{
		var sut = new ValueFeed<int>(Option.Some(42));
		var result = sut.Record();

		await result.WaitForEnd(CT);

		result.Should().Be(r => r
			.Message(42)
		);
	}

	[TestMethod]
	public async Task When_NoneValue_Then_ProducesOneMessageWithNone()
	{
		var sut = new ValueFeed<int>(Option<int>.None());
		var result = sut.Record();

		await result.WaitForEnd(CT);

		result.Should().Be(r => r
			.Message(Data.None)
		);
	}

	[TestMethod]
	public async Task When_UndefinedValue_Then_ProducesOneMessageWithUndefined()
	{
		var sut = new ValueFeed<int>(Option<int>.Undefined());
		var result = sut.Record();

		await result.WaitForEnd(CT);

		result.Should().Be(r => r
			.Message(Data.Undefined)
		);
	}

	[TestMethod]
	public async Task When_StringValue_Then_ProducesOneMessageWithData()
	{
		var sut = new ValueFeed<string>(Option.Some("hello"));
		var result = sut.Record();

		await result.WaitForEnd(CT);

		result.Should().Be(r => r
			.Message("hello")
		);
	}

	[TestMethod]
	public async Task When_ListValue_Then_ProducesOneMessageWithData()
	{
		var list = ImmutableList.Create(1, 2, 3);
		var sut = new ValueFeed<IImmutableList<int>>(Option<IImmutableList<int>>.Some(list));
		var result = sut.Record();

		await result.WaitForEnd(CT);

		result.Count.Should().Be(1);
		result.First().Current.Data.SomeOrDefault()!.Should().BeEquivalentTo(new[] { 1, 2, 3 });
	}
	#endregion

	#region Completion
	[TestMethod]
	public async Task When_Subscribed_Then_ForEachAsyncCompletes()
	{
		var sut = new ValueFeed<int>(Option.Some(42));
		var messageCount = 0;

		await Context.SourceContext
			.GetOrCreateSource(sut)
			.ForEachAsync(msg => { messageCount++; }, CT);

		messageCount.Should().Be(1, "ForEachAsync should complete after the single value is emitted");
	}

	[TestMethod]
	public async Task When_Subscribed_Then_ProducesExactlyOneMessage()
	{
		var sut = new ValueFeed<int>(Option.Some(42));
		var result = sut.Record();

		await result.WaitForEnd(CT);

		result.Count.Should().Be(1, "ValueFeed should produce exactly one message");
	}
	#endregion

	#region Null/default values
	[TestMethod]
	public async Task When_NullableStructNone_Then_ProducesNone()
	{
		var sut = new ValueFeed<int?>(Option<int?>.None());
		var result = sut.Record();

		await result.WaitForEnd(CT);

		result.Should().Be(r => r
			.Message(Data.None)
		);
	}

	[TestMethod]
	public async Task When_NullableStructSome_Then_ProducesValue()
	{
		var sut = new ValueFeed<int?>(Option.Some<int?>(42));
		var result = sut.Record();

		await result.WaitForEnd(CT);

		result.Count.Should().Be(1);
		result.First().Current.Data.SomeOrDefault().Should().Be(42);
	}
	#endregion
}
