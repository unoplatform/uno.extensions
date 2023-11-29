using System;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Testing;

namespace Uno.Extensions.Reactive.Tests.Factories;

[TestClass]
public class Given_SelectAsyncFactories : FeedTests
{
	public class MyService
	{
		public async ValueTask<MyObject> GetObjectAsync<T>(T t, CancellationToken ct) => default!;
		public async ValueTask<MyObject?> GetNullableObjectAsync<T>(T t, CancellationToken ct) => default;

		public async ValueTask<MyStruct> GetStructAsync<T>(T t, CancellationToken ct) => default;
		public async ValueTask<MyStruct?> GetNullableStructAsync<T>(T t, CancellationToken ct) => default!;

		public async ValueTask<string> GetStringAsync<T>(T t, CancellationToken ct) => default!;
		public async ValueTask<string?> GetNullableStringAsync<T>(T t, CancellationToken ct) => default!;

		public async ValueTask<int> GetIntAsync<T>(T t, CancellationToken ct) => default;
		public async ValueTask<int?> GetNullableIntAsync<T>(T t, CancellationToken ct) => default;
	}

	public record MyObject;
	public record struct MyStruct;

	[TestMethod] public Task When_FeedOfObjectSelectAsyncReturnNull_Then_TreatAsNone() => When_SelectAsyncReturnNull_Then_TreatAsNone(Feed.Async(async _ => new MyObject()));
	[TestMethod] public Task When_FeedOfStructSelectAsyncReturnNull_Then_TreatAsNone() => When_SelectAsyncReturnNull_Then_TreatAsNone(Feed.Async(async _ => new MyStruct()));
	[TestMethod] public Task When_FeedOfStringSelectAsyncReturnNull_Then_TreatAsNone() => When_SelectAsyncReturnNull_Then_TreatAsNone(Feed.Async(async _ => ""));
	[TestMethod] public Task When_FeedOfIntSelectAsyncReturnNull_Then_TreatAsNone() => When_SelectAsyncReturnNull_Then_TreatAsNone(Feed.Async(async _ => 42));

	public async Task When_SelectAsyncReturnNull_Then_TreatAsNone<T>(IFeed<T> feed)
	{
		// # Typed infer builders
		// ## Method group
		await feed.SelectAsync(new MyService().GetObjectAsync).Record().Should().BeAsync(m => m.Message(Data.None));
		await feed.SelectAsync(new MyService().GetStructAsync).Record().Should().BeAsync(m => m.Message(Option.Some(default(MyStruct))));
		await feed.SelectAsync(new MyService().GetStringAsync).Record().Should().BeAsync(m => m.Message(Data.None));
		await feed.SelectAsync(new MyService().GetIntAsync).Record().Should().BeAsync(m => m.Message(Option.Some(default(int))));
		//await feed.SelectAsync(new TestService().GetNullableObjectAsync).Record().Should().BeAsync(m => m.Message(Data.None));
		await feed.SelectAsync(new MyService().GetNullableStructAsync).Record().Should().BeAsync(m => m.Message(Data.None)); // Candy
		//await feed.SelectAsync(new TestService().GetNullableStringAsync).Record().Should().BeAsync(m => m.Message(Data.None));
		await feed.SelectAsync(new MyService().GetNullableIntAsync).Record().Should().BeAsync(m => m.Message(Data.None)); // Candy

		// ## Lambdas
		await feed.SelectAsync(async (_, ct) => default(MyObject)!).Record().Should().BeAsync(m => m.Message(Data.None));
		await feed.SelectAsync(async (_, ct) => default(MyStruct)).Record().Should().BeAsync(m => m.Message(Option.Some(default(MyStruct))));
		await feed.SelectAsync(async (_, ct) => default(string)!).Record().Should().BeAsync(m => m.Message(Data.None));
		await feed.SelectAsync(async (_, ct) => default(int)).Record().Should().BeAsync(m => m.Message(Option.Some(default(int))));
		//await feed.SelectAsync(async (_, ct) => default(MyObject?)).Record().Should().BeAsync(m => m.Message(Data.None));
		await feed.SelectAsync(async (_, ct) => default(MyStruct?)).Record().Should().BeAsync(m => m.Message(Data.None)); // Candy
		//await feed.SelectAsync(async (_, ct) => default(string?)).Record().Should().BeAsync(m => m.Message(Data.None));
		await feed.SelectAsync(async (_, ct) => default(int?)).Record().Should().BeAsync(m => m.Message(Data.None)); // Candy
	}
}
