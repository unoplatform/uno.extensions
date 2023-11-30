using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Sources;
using Uno.Extensions.Reactive.Testing;

//namespace System.Runtime.CompilerServices
//{
//	[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
//	file sealed class InterceptsLocationAttribute : Attribute
//	{
//		public InterceptsLocationAttribute(string filePath, int line, int column)
//		{
//		}
//	}
//}

namespace Uno.Extensions.Reactive.Tests
{
	public struct SOmething
	{
		public async IFeed<int> GetMyValue()
		{
			//for (int i = 0; i < 10; i++)
			//{
			//	await Task.Delay(1000);
			//	yield return 10;
			//}

			await Task.Delay(1000);

			var i = GetRandom();

			await Task.Delay(2000);

			i.ToString();

			await Task.Delay(3000);

			return 1;
		}

		public async Task<int> GetRandom()
		{
			await Task.Delay(1000);

			return Random.Shared.Next();
		}
	}

	//public static class Interceptors
	//{
	//	[InterceptsLocationAttribute(@"C:\Src\GitHub\unoplatform\uno.extensions\src\Uno.Extensions.Reactive.Tests\Given_Feed.cs", 98, 31)]
	//	public static Fed<int> GetMyValue()
	//	{
	//		return null!;
	//	}
	//}


	[TestClass]
	public class Given_Feed : FeedTests
	{
		// Those are integration tests that demo the basic public API and are not expected to deeply validate feed behavior.
//#nullable enable
		[TestMethod]
		public async Task When_CustomAsyncMethodBuilderWithStruct()
		{
			var abc = new SOmething().GetMyValue();

			var r = await abc;

			r.ToString();
//#nullable enable
//#pragma warning restore
			IState<int> intState = null!;
			await intState.UpdateAsync(i => i + 1 , CT);

			IState<MyStruct> structState = null!;
			await structState.UpdateAsync(i => i with { Value = 43 }, CT);

			IState<MyStruct> objectState = null!;
			await objectState.UpdateAsync(i => i with { Value = 43 }, CT);
		}

		public record struct MyStruct(int Value);

		public record MyObject(int Value);


		[TestMethod]
		public async Task When_CustomAsyncMethodBuilder()
		{
			var abc = GetMyValue();

			var result = abc.Record();

			await result.WaitForMessages(2);

			await MyState.UpdateAsync(i => i + 1, CT);

			await result.WaitForMessages(4);

			await result.Should().BeAsync(m => m
				.Message(Data.Undefined, Progress.Transient)
				.Message(42, Progress.Final)
				.Message(42, Progress.Transient)
				.Message(43, Progress.Final)
			);
		}

		private IState<int> MyState => State.Value(this, () => 42);

		public async IFeed<int> GetMyValue()
		{
			await Task.Delay(1000);

			//var abc = default(IAsyncDisposable)!;

			//abc.ConfigureAwait(false)

			//return default(int?);
			//return await MyState;
			var i = GetRandom();

			await Task.Delay(2000);

			i.ToString();

			await Task.Delay(3000);

			return 42;
		}

		//public async IFeed<MyObject> GetMyValue2()
		//{
		//	await Task.Delay(1000);

		//	//return await MyState;
		//	return default(MyObject);
		//}

		private readonly MyService _svc = new();
		public class MyService
		{
			public async ValueTask<int> GetMyValue(CancellationToken ct = default) => 42;
		}

		// Actuel:
		public IFeed<int> MyValue => Feed.Async(async _ => 42);

		// Ce qu'on peut faire:
		public async IFeed<int> MyValue2() => 42;


		public async Task<int> GetRandom()
		{
			await Task.Delay(1000);

			return Random.Shared.Next();
		}

		public async ValueTask<int> CreateAValueTask()
		{
			await Task.Delay(1000);

			return 42;
		}

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
			var result = await sut.Option(CT);

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
}
