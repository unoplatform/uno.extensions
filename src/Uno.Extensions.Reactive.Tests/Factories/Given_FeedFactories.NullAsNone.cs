using System;
using System.Linq;
using System.Runtime.CompilerServices;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Testing;

namespace Uno.Extensions.Reactive.Tests.Factories;

[TestClass]
public partial class Given_FeedFactories : FeedTests
{
	public class MyService
	{
		public async ValueTask<MyObject> GetObjectAsync(CancellationToken ct) => default!;
		public async ValueTask<MyObject?> GetNullableObjectAsync(CancellationToken ct) => default;

		public async ValueTask<MyStruct> GetStructAsync(CancellationToken ct) => default;
		public async ValueTask<MyStruct?> GetNullableStructAsync(CancellationToken ct) => default!;

		public async ValueTask<string> GetStringAsync(CancellationToken ct) => default!;
		public async ValueTask<string?> GetNullableStringAsync(CancellationToken ct) => default!;

		public async ValueTask<int> GetIntAsync(CancellationToken ct) => default;
		public async ValueTask<int?> GetNullableIntAsync(CancellationToken ct) => default;

		public async IAsyncEnumerable<MyObject> GetObjectAsyncEnumerable([EnumeratorCancellation] CancellationToken ct) { yield return default!; }
		public async IAsyncEnumerable<MyObject?> GetNullableObjectAsyncEnumerable([EnumeratorCancellation] CancellationToken ct) { yield return default; }

		public async IAsyncEnumerable<MyStruct> GetStructAsyncEnumerable([EnumeratorCancellation] CancellationToken ct) { yield return default; }
		public async IAsyncEnumerable<MyStruct?> GetNullableStructAsyncEnumerable([EnumeratorCancellation] CancellationToken ct) { yield return default!; }

		public async IAsyncEnumerable<string> GetStringAsyncEnumerable([EnumeratorCancellation] CancellationToken ct) { yield return default!; }
		public async IAsyncEnumerable<string?> GetNullableStringAsyncEnumerable([EnumeratorCancellation] CancellationToken ct) { yield return default!; }

		public async IAsyncEnumerable<int> GetIntAsyncEnumerable([EnumeratorCancellation] CancellationToken ct) { yield return default; }
		public async IAsyncEnumerable<int?> GetNullableIntAsyncEnumerable([EnumeratorCancellation] CancellationToken ct) { yield return default; }
	}

	public record MyObject;
	public record struct MyStruct;

	[TestMethod]
	public async Task When_AsyncReturnNull_Then_TreatAsNone()
	{
		// # Typed infer builders
		// ## Method group
		await Feed.Async(new MyService().GetObjectAsync).Record().Should().BeAsync(m => m.Message(Data.None));
		await Feed.Async(new MyService().GetStructAsync).Record().Should().BeAsync(m => m.Message(Option.Some(default(MyStruct))));
		await Feed.Async(new MyService().GetStringAsync).Record().Should().BeAsync(m => m.Message(Data.None));
		await Feed.Async(new MyService().GetIntAsync).Record().Should().BeAsync(m => m.Message(Option.Some(default(int))));
		//await Feed.Async(new TestService().GetNullableObject).Record().Should().BeAsync(m => m.Message(Data.None));
		await Feed.Async(new MyService().GetNullableStructAsync).Record().Should().BeAsync(m => m.Message(Data.None)); // Candy
																												  //await Feed.Async(new TestService().GetNullableString).Record().Should().BeAsync(m => m.Message(Data.None));
		await Feed.Async(new MyService().GetNullableIntAsync).Record().Should().BeAsync(m => m.Message(Data.None)); // Candy

		// ## Lambdas
		await Feed.Async(async _ => default(MyObject)!).Record().Should().BeAsync(m => m.Message(Data.None));
		await Feed.Async(async _ => default(MyStruct)).Record().Should().BeAsync(m => m.Message(Option.Some(default(MyStruct))));
		await Feed.Async(async _ => default(string)!).Record().Should().BeAsync(m => m.Message(Data.None));
		await Feed.Async(async _ => default(int)).Record().Should().BeAsync(m => m.Message(Option.Some(default(int))));
		//await Feed.Async(async _ => default(MyObject?)).Record().Should().BeAsync(m => m.Message(Data.None));
		await Feed.Async(async _ => default(MyStruct?)).Record().Should().BeAsync(m => m.Message(Data.None)); // Candy
																											  //await Feed.Async(async _ => default(string?)).Record().Should().BeAsync(m => m.Message(Data.None));
		await Feed.Async(async _ => default(int?)).Record().Should().BeAsync(m => m.Message(Data.None)); // Candy

		// # Generic builders
		// ## Method group
		await Feed<MyObject>.Async(new MyService().GetObjectAsync).Record().Should().BeAsync(m => m.Message(Data.None));
		await Feed<MyStruct>.Async(new MyService().GetStructAsync).Record().Should().BeAsync(m => m.Message(Option.Some(default(MyStruct))));
		await Feed<string>.Async(new MyService().GetStringAsync).Record().Should().BeAsync(m => m.Message(Data.None));
		await Feed<int>.Async(new MyService().GetIntAsync).Record().Should().BeAsync(m => m.Message(Option.Some(default(int))));
		//await Feed<MyObject>.Async(new TestService().GetNullableObject).Record().Should().BeAsync(m => m.Message(Data.None));
		//await Feed<MyStruct>.Async(new TestService().GetNullableStruct).Record().Should().BeAsync(m => m.Message(Data.None));
		//await Feed<string>.Async(new TestService().GetNullableString).Record().Should().BeAsync(m => m.Message(Data.None));
		//await Feed<int>.Async(new TestService().GetNullableInt).Record().Should().BeAsync(m => m.Message(Data.None));

		//await Feed<MyObject?>.Async(new TestService().GetObject).Record().Should().BeAsync(m => m.Message(Data.None));
		//await Feed<MyStruct?>.Async(new TestService().GetStruct).Record().Should().BeAsync(m => m.Message(Option.Some(default(MyStruct))));;
		//await Feed<string?>.Async(new TestService().GetString).Record().Should().BeAsync(m => m.Message(Data.None));
		//await Feed<int?>.Async(new TestService().GetInt).Record().Should().BeAsync(m => m.Message(Option.Some(default(int))));;
		await Feed<MyObject?>.Async(new MyService().GetNullableObjectAsync).Record().Should().BeAsync(m => m.Message(Data.None));
		await Feed<MyStruct?>.Async(new MyService().GetNullableStructAsync).Record().Should().BeAsync(m => m.Message(Data.None));
		await Feed<string?>.Async(new MyService().GetNullableStringAsync).Record().Should().BeAsync(m => m.Message(Data.None));
		await Feed<int?>.Async(new MyService().GetNullableIntAsync).Record().Should().BeAsync(m => m.Message(Data.None));

		// ## Lambdas
		await Feed<MyObject>.Async(async _ => default(MyObject)!).Record().Should().BeAsync(m => m.Message(Data.None));
		await Feed<MyStruct>.Async(async _ => default(MyStruct)).Record().Should().BeAsync(m => m.Message(Option.Some(default(MyStruct))));
		await Feed<string>.Async(async _ => default(string)!).Record().Should().BeAsync(m => m.Message(Data.None));
		await Feed<int>.Async(async _ => default(int)).Record().Should().BeAsync(m => m.Message(Option.Some(default(int))));
		//await Feed<MyObject>.Async(async _ => default(MyObject?)).Record().Should().BeAsync(m => m.Message(Data.None));
		//await Feed<MyStruct>.Async(async _ => default(MyStruct?)).Record().Should().BeAsync(m => m.Message(Data.None));
		//await Feed<string>.Async(async _ => default(string?)).Record().Should().BeAsync(m => m.Message(Data.None));
		//await Feed<int>.Async(async _ => default(int?)).Record().Should().BeAsync(m => m.Message(Data.None));

		//await Feed<MyObject?>.Async(async _ => default(MyObject)).Record().Should().BeAsync(m => m.Message(Data.None));
		//await Feed<MyStruct?>.Async(async _ => default(MyStruct)).Record().Should().BeAsync(m => m.Message(Option.Some(default(MyStruct))));
		//await Feed<string?>.Async(async _ => default(string)).Record().Should().BeAsync(m => m.Message(Data.None));
		//await Feed<int?>.Async(async _ => default(int)).Record().Should().BeAsync(m => m.Message(Option.Some(default(int))));
		await Feed<MyObject?>.Async(async _ => default(MyObject?)).Record().Should().BeAsync(m => m.Message(Data.None));
		await Feed<MyStruct?>.Async(async _ => default(MyStruct?)).Record().Should().BeAsync(m => m.Message(Data.None));
		await Feed<string?>.Async(async _ => default(string?)).Record().Should().BeAsync(m => m.Message(Data.None));
		await Feed<int?>.Async(async _ => default(int?)).Record().Should().BeAsync(m => m.Message(Data.None));
	}

	[TestMethod]
	public async Task When_AsyncEnumerableReturnNull_Then_TreatAsNone()
	{
		// # Typed infer builders
		// ## Method group
		await Feed.AsyncEnumerable(new MyService().GetObjectAsyncEnumerable).Record().Should().BeAsync(m => m.Message(Data.None));
		await Feed.AsyncEnumerable(new MyService().GetStructAsyncEnumerable).Record().Should().BeAsync(m => m.Message(Option.Some(default(MyStruct))));
		await Feed.AsyncEnumerable(new MyService().GetStringAsyncEnumerable).Record().Should().BeAsync(m => m.Message(Data.None));
		await Feed.AsyncEnumerable(new MyService().GetIntAsyncEnumerable).Record().Should().BeAsync(m => m.Message(Option.Some(default(int))));
		//await Feed.AsyncEnumerable(new TestService().GetNullableObject).Record().Should().BeAsync(m => m.Message(Data.None));
		await Feed.AsyncEnumerable(new MyService().GetNullableStructAsyncEnumerable).Record().Should().BeAsync(m => m.Message(Data.None)); // Candy
																																		   //await Feed.AsyncEnumerable(new TestService().GetNullableString).Record().Should().BeAsync(m => m.Message(Data.None));
		await Feed.AsyncEnumerable(new MyService().GetNullableIntAsyncEnumerable).Record().Should().BeAsync(m => m.Message(Data.None)); // Candy

		// # Generic builders
		// ## Method group
		await Feed<MyObject>.AsyncEnumerable(new MyService().GetObjectAsyncEnumerable).Record().Should().BeAsync(m => m.Message(Data.None));
		await Feed<MyStruct>.AsyncEnumerable(new MyService().GetStructAsyncEnumerable).Record().Should().BeAsync(m => m.Message(Option.Some(default(MyStruct))));
		await Feed<string>.AsyncEnumerable(new MyService().GetStringAsyncEnumerable).Record().Should().BeAsync(m => m.Message(Data.None));
		await Feed<int>.AsyncEnumerable(new MyService().GetIntAsyncEnumerable).Record().Should().BeAsync(m => m.Message(Option.Some(default(int))));
		//await Feed<MyObject>.AsyncEnumerable(new TestService().GetNullableObject).Record().Should().BeAsync(m => m.Message(Data.None));
		//await Feed<MyStruct>.AsyncEnumerable(new TestService().GetNullableStruct).Record().Should().BeAsync(m => m.Message(Data.None));
		//await Feed<string>.AsyncEnumerable(new TestService().GetNullableString).Record().Should().BeAsync(m => m.Message(Data.None));
		//await Feed<int>.AsyncEnumerable(new TestService().GetNullableInt).Record().Should().BeAsync(m => m.Message(Data.None));

		//await Feed<MyObject?>.AsyncEnumerable(new TestService().GetObject).Record().Should().BeAsync(m => m.Message(Data.None));
		//await Feed<MyStruct?>.AsyncEnumerable(new TestService().GetStruct).Record().Should().BeAsync(m => m.Message(Option.Some(default(MyStruct))));;
		//await Feed<string?>.AsyncEnumerable(new TestService().GetString).Record().Should().BeAsync(m => m.Message(Data.None));
		//await Feed<int?>.AsyncEnumerable(new TestService().GetInt).Record().Should().BeAsync(m => m.Message(Option.Some(default(int))));;
		await Feed<MyObject?>.AsyncEnumerable(new MyService().GetNullableObjectAsyncEnumerable).Record().Should().BeAsync(m => m.Message(Data.None));
		await Feed<MyStruct?>.AsyncEnumerable(new MyService().GetNullableStructAsyncEnumerable).Record().Should().BeAsync(m => m.Message(Data.None));
		await Feed<string?>.AsyncEnumerable(new MyService().GetNullableStringAsyncEnumerable).Record().Should().BeAsync(m => m.Message(Data.None));
		await Feed<int?>.AsyncEnumerable(new MyService().GetNullableIntAsyncEnumerable).Record().Should().BeAsync(m => m.Message(Data.None));
	}
}
