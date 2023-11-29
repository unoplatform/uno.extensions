using System;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Testing;

namespace Uno.Extensions.Reactive.Tests.Factories;

[TestClass]
public class Given_SelectFactories : FeedTests
{
	public class MyService
	{
		public MyObject GetObject<T>(T t) => default!;
		public MyObject? GetNullableObject<T>(T t) => default;

		public MyStruct GetStruct<T>(T t) => default;
		public MyStruct? GetNullableStruct<T>(T t) => default!;

		public string GetString<T>(T t) => default!;
		public string? GetNullableString<T>(T t) => default!;

		public int GetInt<T>(T t) => default;
		public int? GetNullableInt<T>(T t) => default;
	}

	public record MyObject;
	public record struct MyStruct;

	[TestMethod] public Task When_FeedOfObjectSelectReturnNull_Then_TreatAsNone() => When_SelectReturnNull_Then_TreatAsNone(Feed.Async(async _ => new MyObject()));
	[TestMethod] public Task When_FeedOfStructSelectReturnNull_Then_TreatAsNone() => When_SelectReturnNull_Then_TreatAsNone(Feed.Async(async _ => new MyStruct()));
	[TestMethod] public Task When_FeedOfStringSelectReturnNull_Then_TreatAsNone() => When_SelectReturnNull_Then_TreatAsNone(Feed.Async(async _ => ""));
	[TestMethod] public Task When_FeedOfIntSelectReturnNull_Then_TreatAsNone() => When_SelectReturnNull_Then_TreatAsNone(Feed.Async(async _ => 42));

	public async Task When_SelectReturnNull_Then_TreatAsNone<T>(IFeed<T> feed)
	{
		// # Typed infer builders
		// ## Method group
		await feed.Select(new MyService().GetObject).Record().Should().BeAsync(m => m.Message(Data.None));
		await feed.Select(new MyService().GetStruct).Record().Should().BeAsync(m => m.Message(Option.Some(default(MyStruct))));
		await feed.Select(new MyService().GetString).Record().Should().BeAsync(m => m.Message(Data.None));
		await feed.Select(new MyService().GetInt).Record().Should().BeAsync(m => m.Message(Option.Some(default(int))));
		//await feed.Select(new TestService().GetNullableObject).Record().Should().BeAsync(m => m.Message(Data.None));
		await feed.Select(new MyService().GetNullableStruct).Record().Should().BeAsync(m => m.Message(Data.None)); // Candy
		//await feed.Select(new TestService().GetNullableString).Record().Should().BeAsync(m => m.Message(Data.None));
		await feed.Select(new MyService().GetNullableInt).Record().Should().BeAsync(m => m.Message(Data.None)); // Candy

		// ## Lambdas
		await feed.Select(_ => default(MyObject)!).Record().Should().BeAsync(m => m.Message(Data.None));
		await feed.Select(_ => default(MyStruct)).Record().Should().BeAsync(m => m.Message(Option.Some(default(MyStruct))));
		await feed.Select(_ => default(string)!).Record().Should().BeAsync(m => m.Message(Data.None));
		await feed.Select(_ => default(int)).Record().Should().BeAsync(m => m.Message(Option.Some(default(int))));
		//await feed.Select(_ => default(MyObject?)).Record().Should().BeAsync(m => m.Message(Data.None));
		await feed.Select(_ => default(MyStruct?)).Record().Should().BeAsync(m => m.Message(Data.None)); // Candy
		//await feed.Select(_ => default(string?)).Record().Should().BeAsync(m => m.Message(Data.None));
		await feed.Select(_ => default(int?)).Record().Should().BeAsync(m => m.Message(Data.None)); // Candy
	}
}
