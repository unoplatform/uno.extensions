using System;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Testing;

namespace Uno.Extensions.Reactive.Tests.Factories;

[TestClass]
public class Given_WhereNotNullFactories : FeedTests
{
	public record MyObject;
	public record struct MyStruct;

	[TestMethod]
	public async Task When_InputIsSomeNull_Then_TreatAsNone()
	{
		await Feed<MyObject?>.Async(async _ => default(MyObject?)).WhereNotNull().Record().Should().BeAsync(m => m.Message(Data.None));
		await Feed<MyStruct?>.Async(async _ => default(MyStruct?)).WhereNotNull().Record().Should().BeAsync(m => m.Message(Data.None));
		await Feed<string?>.Async(async _ => default(string?)).WhereNotNull().Record().Should().BeAsync(m => m.Message(Data.None));
		await Feed<int?>.Async(async _ => default(int?)).WhereNotNull().Record().Should().BeAsync(m => m.Message(Data.None));
	}

	[TestMethod]
	public async Task When_InputIsSomeNotNull_Then_StaySome()
	{
		await Feed<MyObject?>.Async(async _ => new MyObject()).WhereNotNull().Record().Should().BeAsync(m => m.Message(Data.Some));
		await Feed<MyStruct?>.Async(async _ => new MyStruct()).WhereNotNull().Record().Should().BeAsync(m => m.Message(Data.Some));
		await Feed<string?>.Async(async _ => "").WhereNotNull().Record().Should().BeAsync(m => m.Message(""));
		await Feed<int?>.Async(async _ => 42).WhereNotNull().Record().Should().BeAsync(m => m.Message(42));
	}

	[TestMethod]
	public async Task When_InputUndefined_Then_StayUndefined()
	{
		await Feed<MyObject?>.Async(async _ => Option<MyObject?>.Undefined()).WhereNotNull().Record().Should().BeAsync(m => m.Message(Data.Undefined));
		await Feed<MyStruct?>.Async(async _ => Option<MyStruct?>.Undefined()).WhereNotNull().Record().Should().BeAsync(m => m.Message(Data.Undefined));
		await Feed<string?>.Async(async _ => Option<string?>.Undefined()).WhereNotNull().Record().Should().BeAsync(m => m.Message(Data.Undefined));
		await Feed<int?>.Async(async _ => Option<int?>.Undefined()).WhereNotNull().Record().Should().BeAsync(m => m.Message(Data.Undefined));
	}

	[TestMethod]
	public async Task When_InputIsNone_Then_StayNone()
	{
		await Feed<MyObject?>.Async(async _ => Option<MyObject?>.None()).WhereNotNull().Record().Should().BeAsync(m => m.Message(Data.None));
		await Feed<MyStruct?>.Async(async _ => Option<MyStruct?>.None()).WhereNotNull().Record().Should().BeAsync(m => m.Message(Data.None));
		await Feed<string?>.Async(async _ => Option<string?>.None()).WhereNotNull().Record().Should().BeAsync(m => m.Message(Data.None));
		await Feed<int?>.Async(async _ => Option<int?>.None()).WhereNotNull().Record().Should().BeAsync(m => m.Message(Data.None));
	}
}
