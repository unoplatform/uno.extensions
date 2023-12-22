using System;
using System.Linq;
using System.Runtime.CompilerServices;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Testing;

namespace Uno.Extensions.Reactive.Tests.Factories;

public partial class Given_StateFactories : FeedTests
{
	public class MyService
	{
		public MyObject GetObject() => default!;
		public MyObject? GetNullableObject() => default;

		public MyStruct GetStruct() => default;
		public MyStruct? GetNullableStruct() => default!;

		public string GetString() => default!;
		public string? GetNullableString() => default!;

		public int GetInt() => default;
		public int? GetNullableInt() => default;


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
	public async Task When_ValueReturnNull_Then_TreatAsNone()
	{
		// # Typed infer builders
		// ## Method group
		await State.Value(this, new MyService().GetObject).Record().Should().BeAsync(m => m.Message(Data.None));
		await State.Value(this, new MyService().GetStruct).Record().Should().BeAsync(m => m.Message(Option.Some(default(MyStruct))));
		await State.Value(this, new MyService().GetString).Record().Should().BeAsync(m => m.Message(Data.None));
		await State.Value(this, new MyService().GetInt).Record().Should().BeAsync(m => m.Message(Option.Some(default(int))));
		//await State.Value(this, new TestService().GetNullableObject).Record().Should().BeAsync(m => m.Message(Data.None));
		await State.Value(this, new MyService().GetNullableStruct).Record().Should().BeAsync(m => m.Message(Data.None)); // Candy
																															  //await State.Value(this, new TestService().GetNullableString).Record().Should().BeAsync(m => m.Message(Data.None));
		await State.Value(this, new MyService().GetNullableInt).Record().Should().BeAsync(m => m.Message(Data.None)); // Candy

		// ## Lambdas
		await State.Value(this, () => default(MyObject)!).Record().Should().BeAsync(m => m.Message(Data.None));
		await State.Value(this, () => default(MyStruct)).Record().Should().BeAsync(m => m.Message(Option.Some(default(MyStruct))));
		await State.Value(this, () => default(string)!).Record().Should().BeAsync(m => m.Message(Data.None));
		await State.Value(this, () => default(int)).Record().Should().BeAsync(m => m.Message(Option.Some(default(int))));
		//await State.Value(this, () => default(MyObject?)).Record().Should().BeAsync(m => m.Message(Data.None));
		await State.Value(this, () => default(MyStruct?)).Record().Should().BeAsync(m => m.Message(Data.None)); // Candy
																													 //await State.Value(this, () => default(string?)).Record().Should().BeAsync(m => m.Message(Data.None));
		await State.Value(this, () => default(int?)).Record().Should().BeAsync(m => m.Message(Data.None)); // Candy

		// # Generic builders
		// ## Method group
		await State<MyObject>.Value(this, new MyService().GetObject).Record().Should().BeAsync(m => m.Message(Data.None));
		await State<MyStruct>.Value(this, new MyService().GetStruct).Record().Should().BeAsync(m => m.Message(Option.Some(default(MyStruct))));
		await State<string>.Value(this, new MyService().GetString).Record().Should().BeAsync(m => m.Message(Data.None));
		await State<int>.Value(this, new MyService().GetInt).Record().Should().BeAsync(m => m.Message(Option.Some(default(int))));
		//await State<MyObject>.Value(this, new TestService().GetNullableObject).Record().Should().BeAsync(m => m.Message(Data.None));
		//await State<MyStruct>.Value(this, new TestService().GetNullableStruct).Record().Should().BeAsync(m => m.Message(Data.None));
		//await State<string>.Value(this, new TestService().GetNullableString).Record().Should().BeAsync(m => m.Message(Data.None));
		//await State<int>.Value(this, new TestService().GetNullableInt).Record().Should().BeAsync(m => m.Message(Data.None));

		//await State<MyObject?>.Value(this, new TestService().GetObject).Record().Should().BeAsync(m => m.Message(Data.None));
		//await State<MyStruct?>.Value(this, new TestService().GetStruct).Record().Should().BeAsync(m => m.Message(Option.Some(default(MyStruct))));;
		//await State<string?>.Value(this, new TestService().GetString).Record().Should().BeAsync(m => m.Message(Data.None));
		//await State<int?>.Value(this, new TestService().GetInt).Record().Should().BeAsync(m => m.Message(Option.Some(default(int))));;
		await State<MyObject?>.Value(this, new MyService().GetNullableObject).Record().Should().BeAsync(m => m.Message(Data.None));
		await State<MyStruct?>.Value(this, new MyService().GetNullableStruct).Record().Should().BeAsync(m => m.Message(Data.None));
		await State<string?>.Value(this, new MyService().GetNullableString).Record().Should().BeAsync(m => m.Message(Data.None));
		await State<int?>.Value(this, new MyService().GetNullableInt).Record().Should().BeAsync(m => m.Message(Data.None));

		// ## Lambdas
		await State<MyObject>.Value(this, () => default(MyObject)!).Record().Should().BeAsync(m => m.Message(Data.None));
		await State<MyStruct>.Value(this, () => default(MyStruct)).Record().Should().BeAsync(m => m.Message(Option.Some(default(MyStruct))));
		await State<string>.Value(this, () => default(string)!).Record().Should().BeAsync(m => m.Message(Data.None));
		await State<int>.Value(this, () => default(int)).Record().Should().BeAsync(m => m.Message(Option.Some(default(int))));
		//await State<MyObject>.Value(this, () => default(MyObject?)).Record().Should().BeAsync(m => m.Message(Data.None));
		//await State<MyStruct>.Value(this, () => default(MyStruct?)).Record().Should().BeAsync(m => m.Message(Data.None));
		//await State<string>.Value(this, () => default(string?)).Record().Should().BeAsync(m => m.Message(Data.None));
		//await State<int>.Value(this, () => default(int?)).Record().Should().BeAsync(m => m.Message(Data.None));

		//await State<MyObject?>.Value(this, () => default(MyObject)).Record().Should().BeAsync(m => m.Message(Data.None));
		//await State<MyStruct?>.Value(this, () => default(MyStruct)).Record().Should().BeAsync(m => m.Message(Option.Some(default(MyStruct))));
		//await State<string?>.Value(this, () => default(string)).Record().Should().BeAsync(m => m.Message(Data.None));
		//await State<int?>.Value(this, () => default(int)).Record().Should().BeAsync(m => m.Message(Option.Some(default(int))));
		await State<MyObject?>.Value(this, () => default(MyObject?)).Record().Should().BeAsync(m => m.Message(Data.None));
		await State<MyStruct?>.Value(this, () => default(MyStruct?)).Record().Should().BeAsync(m => m.Message(Data.None));
		await State<string?>.Value(this, () => default(string?)).Record().Should().BeAsync(m => m.Message(Data.None));
		await State<int?>.Value(this, () => default(int?)).Record().Should().BeAsync(m => m.Message(Data.None));
	}

	[TestMethod]
	public async Task When_AsyncReturnNull_Then_TreatAsNone()
	{
		// # Typed infer builders
		// ## Method group
		await State.Async(this, new MyService().GetObjectAsync).Record().Should().BeAsync(m => m.Message(Data.None));
		await State.Async(this, new MyService().GetStructAsync).Record().Should().BeAsync(m => m.Message(Option.Some(default(MyStruct))));
		await State.Async(this, new MyService().GetStringAsync).Record().Should().BeAsync(m => m.Message(Data.None));
		await State.Async(this, new MyService().GetIntAsync).Record().Should().BeAsync(m => m.Message(Option.Some(default(int))));
		//await State.Async(this, new TestService().GetNullableObject).Record().Should().BeAsync(m => m.Message(Data.None));
		await State.Async(this, new MyService().GetNullableStructAsync).Record().Should().BeAsync(m => m.Message(Data.None)); // Candy
																												  //await State.Async(this, new TestService().GetNullableString).Record().Should().BeAsync(m => m.Message(Data.None));
		await State.Async(this, new MyService().GetNullableIntAsync).Record().Should().BeAsync(m => m.Message(Data.None)); // Candy

		// ## Lambdas
		await State.Async(this, async _ => default(MyObject)!).Record().Should().BeAsync(m => m.Message(Data.None));
		await State.Async(this, async _ => default(MyStruct)).Record().Should().BeAsync(m => m.Message(Option.Some(default(MyStruct))));
		await State.Async(this, async _ => default(string)!).Record().Should().BeAsync(m => m.Message(Data.None));
		await State.Async(this, async _ => default(int)).Record().Should().BeAsync(m => m.Message(Option.Some(default(int))));
		//await State.Async(this, async _ => default(MyObject?)).Record().Should().BeAsync(m => m.Message(Data.None));
		await State.Async(this, async _ => default(MyStruct?)).Record().Should().BeAsync(m => m.Message(Data.None)); // Candy
																											  //await State.Async(this, async _ => default(string?)).Record().Should().BeAsync(m => m.Message(Data.None));
		await State.Async(this, async _ => default(int?)).Record().Should().BeAsync(m => m.Message(Data.None)); // Candy

		// # Generic builders
		// ## Method group
		await State<MyObject>.Async(this, new MyService().GetObjectAsync).Record().Should().BeAsync(m => m.Message(Data.None));
		await State<MyStruct>.Async(this, new MyService().GetStructAsync).Record().Should().BeAsync(m => m.Message(Option.Some(default(MyStruct))));
		await State<string>.Async(this, new MyService().GetStringAsync).Record().Should().BeAsync(m => m.Message(Data.None));
		await State<int>.Async(this, new MyService().GetIntAsync).Record().Should().BeAsync(m => m.Message(Option.Some(default(int))));
		//await State<MyObject>.Async(this, new TestService().GetNullableObject).Record().Should().BeAsync(m => m.Message(Data.None));
		//await State<MyStruct>.Async(this, new TestService().GetNullableStruct).Record().Should().BeAsync(m => m.Message(Data.None));
		//await State<string>.Async(this, new TestService().GetNullableString).Record().Should().BeAsync(m => m.Message(Data.None));
		//await State<int>.Async(this, new TestService().GetNullableInt).Record().Should().BeAsync(m => m.Message(Data.None));

		//await State<MyObject?>.Async(this, new TestService().GetObject).Record().Should().BeAsync(m => m.Message(Data.None));
		//await State<MyStruct?>.Async(this, new TestService().GetStruct).Record().Should().BeAsync(m => m.Message(Option.Some(default(MyStruct))));;
		//await State<string?>.Async(this, new TestService().GetString).Record().Should().BeAsync(m => m.Message(Data.None));
		//await State<int?>.Async(this, new TestService().GetInt).Record().Should().BeAsync(m => m.Message(Option.Some(default(int))));;
		await State<MyObject?>.Async(this, new MyService().GetNullableObjectAsync).Record().Should().BeAsync(m => m.Message(Data.None));
		await State<MyStruct?>.Async(this, new MyService().GetNullableStructAsync).Record().Should().BeAsync(m => m.Message(Data.None));
		await State<string?>.Async(this, new MyService().GetNullableStringAsync).Record().Should().BeAsync(m => m.Message(Data.None));
		await State<int?>.Async(this, new MyService().GetNullableIntAsync).Record().Should().BeAsync(m => m.Message(Data.None));

		// ## Lambdas
		await State<MyObject>.Async(this, async _ => default(MyObject)!).Record().Should().BeAsync(m => m.Message(Data.None));
		await State<MyStruct>.Async(this, async _ => default(MyStruct)).Record().Should().BeAsync(m => m.Message(Option.Some(default(MyStruct))));
		await State<string>.Async(this, async _ => default(string)!).Record().Should().BeAsync(m => m.Message(Data.None));
		await State<int>.Async(this, async _ => default(int)).Record().Should().BeAsync(m => m.Message(Option.Some(default(int))));
		//await State<MyObject>.Async(this, async _ => default(MyObject?)).Record().Should().BeAsync(m => m.Message(Data.None));
		//await State<MyStruct>.Async(this, async _ => default(MyStruct?)).Record().Should().BeAsync(m => m.Message(Data.None));
		//await State<string>.Async(this, async _ => default(string?)).Record().Should().BeAsync(m => m.Message(Data.None));
		//await State<int>.Async(this, async _ => default(int?)).Record().Should().BeAsync(m => m.Message(Data.None));

		//await State<MyObject?>.Async(this, async _ => default(MyObject)).Record().Should().BeAsync(m => m.Message(Data.None));
		//await State<MyStruct?>.Async(this, async _ => default(MyStruct)).Record().Should().BeAsync(m => m.Message(Option.Some(default(MyStruct))));
		//await State<string?>.Async(this, async _ => default(string)).Record().Should().BeAsync(m => m.Message(Data.None));
		//await State<int?>.Async(this, async _ => default(int)).Record().Should().BeAsync(m => m.Message(Option.Some(default(int))));
		await State<MyObject?>.Async(this, async _ => default(MyObject?)).Record().Should().BeAsync(m => m.Message(Data.None));
		await State<MyStruct?>.Async(this, async _ => default(MyStruct?)).Record().Should().BeAsync(m => m.Message(Data.None));
		await State<string?>.Async(this, async _ => default(string?)).Record().Should().BeAsync(m => m.Message(Data.None));
		await State<int?>.Async(this, async _ => default(int?)).Record().Should().BeAsync(m => m.Message(Data.None));
	}

	[TestMethod]
	public async Task When_AsyncEnumerableReturnNull_Then_TreatAsNone()
	{
		// # Typed infer builders
		// ## Method group
		await State.AsyncEnumerable(this, new MyService().GetObjectAsyncEnumerable).Record().Should().BeAsync(m => m.Message(Data.None));
		await State.AsyncEnumerable(this, new MyService().GetStructAsyncEnumerable).Record().Should().BeAsync(m => m.Message(Option.Some(default(MyStruct))));
		await State.AsyncEnumerable(this, new MyService().GetStringAsyncEnumerable).Record().Should().BeAsync(m => m.Message(Data.None));
		await State.AsyncEnumerable(this, new MyService().GetIntAsyncEnumerable).Record().Should().BeAsync(m => m.Message(Option.Some(default(int))));
		//await State.AsyncEnumerable(this, new TestService().GetNullableObject).Record().Should().BeAsync(m => m.Message(Data.None));
		await State.AsyncEnumerable(this, new MyService().GetNullableStructAsyncEnumerable).Record().Should().BeAsync(m => m.Message(Data.None)); // Candy
																																		   //await State.AsyncEnumerable(this, new TestService().GetNullableString).Record().Should().BeAsync(m => m.Message(Data.None));
		await State.AsyncEnumerable(this, new MyService().GetNullableIntAsyncEnumerable).Record().Should().BeAsync(m => m.Message(Data.None)); // Candy

		// # Generic builders
		// ## Method group
		await State<MyObject>.AsyncEnumerable(this, new MyService().GetObjectAsyncEnumerable).Record().Should().BeAsync(m => m.Message(Data.None));
		await State<MyStruct>.AsyncEnumerable(this, new MyService().GetStructAsyncEnumerable).Record().Should().BeAsync(m => m.Message(Option.Some(default(MyStruct))));
		await State<string>.AsyncEnumerable(this, new MyService().GetStringAsyncEnumerable).Record().Should().BeAsync(m => m.Message(Data.None));
		await State<int>.AsyncEnumerable(this, new MyService().GetIntAsyncEnumerable).Record().Should().BeAsync(m => m.Message(Option.Some(default(int))));
		//await State<MyObject>.AsyncEnumerable(this, new TestService().GetNullableObject).Record().Should().BeAsync(m => m.Message(Data.None));
		//await State<MyStruct>.AsyncEnumerable(this, new TestService().GetNullableStruct).Record().Should().BeAsync(m => m.Message(Data.None));
		//await State<string>.AsyncEnumerable(this, new TestService().GetNullableString).Record().Should().BeAsync(m => m.Message(Data.None));
		//await State<int>.AsyncEnumerable(this, new TestService().GetNullableInt).Record().Should().BeAsync(m => m.Message(Data.None));

		//await State<MyObject?>.AsyncEnumerable(this, new TestService().GetObject).Record().Should().BeAsync(m => m.Message(Data.None));
		//await State<MyStruct?>.AsyncEnumerable(this, new TestService().GetStruct).Record().Should().BeAsync(m => m.Message(Option.Some(default(MyStruct))));;
		//await State<string?>.AsyncEnumerable(this, new TestService().GetString).Record().Should().BeAsync(m => m.Message(Data.None));
		//await State<int?>.AsyncEnumerable(this, new TestService().GetInt).Record().Should().BeAsync(m => m.Message(Option.Some(default(int))));;
		await State<MyObject?>.AsyncEnumerable(this, new MyService().GetNullableObjectAsyncEnumerable).Record().Should().BeAsync(m => m.Message(Data.None));
		await State<MyStruct?>.AsyncEnumerable(this, new MyService().GetNullableStructAsyncEnumerable).Record().Should().BeAsync(m => m.Message(Data.None));
		await State<string?>.AsyncEnumerable(this, new MyService().GetNullableStringAsyncEnumerable).Record().Should().BeAsync(m => m.Message(Data.None));
		await State<int?>.AsyncEnumerable(this, new MyService().GetNullableIntAsyncEnumerable).Record().Should().BeAsync(m => m.Message(Data.None));
	}
}
