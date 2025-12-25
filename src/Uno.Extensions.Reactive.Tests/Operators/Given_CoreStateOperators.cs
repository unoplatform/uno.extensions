using System;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Uno.Extensions.Reactive.Testing;

namespace Uno.Extensions.Reactive.Tests.Operators;

[TestClass]
public class Given_CoreStateOperators : FeedTests
{
	#region UpdateAsync
	[TestMethod]
	// TODO: Expect null ref exception
	public async Task When_UpdateAsync_Then_AcceptsNotNullAndStruct()
	{
		// Note: This is a compilation tests!
		await default(IState<int>)!.UpdateAsync(_ => 42, CT);
		await default(IState<int?>)!.UpdateAsync(_ => default(int?), CT);
		await default(IState<string>)!.UpdateAsync(_ => "", CT);
#nullable disable
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
		await default(IState<string?>)!.UpdateAsync(_ => default(string?), CT);
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#nullable restore
		await default(IState<MyStruct>)!.UpdateAsync(_ => new MyStruct(), CT);
		await default(IState<MyStruct?>)!.UpdateAsync(_ => default(MyStruct?), CT);
		await default(IState<MyClass>)!.UpdateAsync(_ => new MyClass(), CT);
#nullable disable
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
		await default(IState<MyClass?>)!.UpdateAsync(_ => default(MyClass?), CT);
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
#nullable restore
	}

	[TestMethod]
	public async Task When_UpdateAsyncNullStateOfInt_Then_TreatAsNone()
	{
		var state = State<int>.Value(this, () => 42);
		var result = state.Record();

		await state.UpdateAsync(_ => null, CT);

		result.Should().Be(m => m
			.Message(42)
			.Message(Data.None)
		);
	}

	[TestMethod]
	public async Task When_UpdateAsyncNullStateOfNullableInt_Then_TreatAsNone()
	{
		var state = State<int?>.Value(this, () => 42);
		var result = state.Record();

		await state.UpdateAsync(_ => null, CT);

		result.Should().Be(m => m
			.Message(42)
			.Message(Data.None)
		);
	}

	[TestMethod]
	public async Task When_UpdateAsyncNullStateOfString_Then_TreatAsNone()
	{
		var state = State<string>.Value(this, () => "42");
		var result = state.Record();

		await state.UpdateAsync(_ => null, CT);

		result.Should().Be(m => m
			.Message("42")
			.Message(Data.None)
		);
	}

	//[TestMethod] // Forbidden, need to use UpdateDataAsync instead
	//public async Task When_UpdateAsyncNullStateOfNullableString_Then_TreatAsNone()
	//{
	//	var state = State<string?>.Value(this, () => "42");
	//	var result = state.Record();

	//	await state.UpdateAsync(_ => null, CT);

	//	result.Should().Be(m => m
	//		.Message("42")
	//		.Message(Data.None)
	//	);
	//}

	[TestMethod]
	public async Task When_UpdateAsyncNullStateOfStruct_Then_TreatAsNone()
	{
		var state = State<MyStruct>.Value(this, () => new MyStruct());
		var result = state.Record();

		await state.UpdateAsync(_ => null, CT);

		result.Should().Be(m => m
			.Message(Data.Some)
			.Message(Data.None)
		);
	}

	[TestMethod]
	public async Task When_UpdateAsyncNullStateOfNullableStruct_Then_TreatAsNone()
	{
		var state = State<MyStruct?>.Value(this, () => new MyStruct());
		var result = state.Record();

		await state.UpdateAsync(_ => null, CT);

		result.Should().Be(m => m
			.Message(Data.Some)
			.Message(Data.None)
		);
	}

	[TestMethod]
	public async Task When_UpdateAsyncNullStateOfObject_Then_TreatAsNone()
	{
		var state = State<MyClass>.Value(this, () => new MyClass());
		var result = state.Record();

		await state.UpdateAsync(_ => null, CT);

		result.Should().Be(m => m
			.Message(Data.Some)
			.Message(Data.None));
	}

	//[TestMethod] // Forbidden, need to use UpdateDataAsync instead
	//public async Task When_UpdateAsyncNullStateOfNullableObject_Then_TreatAsNone()
	//{
	//	var state = State<MyClass?>.Value(this, () => new MyClass());
	//	var result = state.Record();

	//	await state.UpdateAsync(_ => null, CT);

	//	result.Should().Be(m => m
	//		.Message(Data.Some)
	//		.Message(Data.None));
	//} 
	#endregion

	#region UpdateDataAsync
	[TestMethod]
	// TODO: Expect null ref exception
	public async Task When_UpdateDataAsync_Then_AcceptsNotNullAndStruct()
	{
		// Note: This is a compilation tests!
		await default(IState<int>)!.UpdateDataAsync(_ => 42, CT);
		await default(IState<int?>)!.UpdateDataAsync(_ => default(int?), CT);
		await default(IState<string>)!.UpdateDataAsync(_ => "", CT);
		await default(IState<string?>)!.UpdateDataAsync(_ => default(string?), CT);
		await default(IState<MyStruct>)!.UpdateDataAsync(_ => new MyStruct(), CT);
		await default(IState<MyStruct?>)!.UpdateDataAsync(_ => default(MyStruct?), CT);
		await default(IState<MyClass>)!.UpdateDataAsync(_ => new MyClass(), CT);
		await default(IState<MyClass?>)!.UpdateDataAsync(_ => default(MyClass?), CT);
	}

	[TestMethod]
	public async Task When_UpdateDataAsyncReturnsDefaultOptionStateOfInt_Then_TreatAsNone()
	{
		var state = State<int>.Value(this, () => 42);
		var result = state.Record();

		await state.UpdateDataAsync(_ => default(Option<int>), CT);

		result.Should().Be(m => m
			.Message(42)
			.Message(Data.None)
		);
	}

	[TestMethod]
	public async Task When_UpdateDataAsyncReturnsDefaultValueStateOfInt_Then_TreatAsSome()
	{
		var state = State<int>.Value(this, () => 42);
		var result = state.Record();

		await state.UpdateDataAsync(_ => default(int), CT);

		result.Should().Be(m => m
			.Message(42)
			.Message(0)
		);
	}

	[TestMethod]
	public async Task When_UpdateDataAsyncReturnsDefaultOptionStateOfNullableInt_Then_TreatAsNone()
	{
		var state = State<int?>.Value(this, () => 42);
		var result = state.Record();

		await state.UpdateDataAsync(_ => default(Option<int?>), CT);

		result.Should().Be(m => m
			.Message(42)
			.Message(Data.None)
		);
	}

	[TestMethod]
	public async Task When_UpdateDataAsyncReturnsNullStateOfNullableInt_Then_TreatAsSome()
	{
		var state = State<int?>.Value(this, () => 42);
		var result = state.Record();

		await state.UpdateDataAsync(_ => default(int?), CT);

		result.Should().Be(m => m
			.Message(42)
			.Message(Option.Some(default(int?)))
		);
	}

	[TestMethod]
	public async Task When_UpdateDataAsyncReturnsDefaultOptionStateOfString_Then_TreatAsNone()
	{
		var state = State<string>.Value(this, () => "42");
		var result = state.Record();

		await state.UpdateDataAsync(_ => default(Option<string>), CT);

		result.Should().Be(m => m
			.Message("42")
			.Message(Data.None)
		);
	}

	[TestMethod]
	public async Task When_UpdateDataAsyncReturnsDefaultValueStateOfString_Then_TreatAsSome()
	{
		var state = State<string>.Value(this, () => "42");
		var result = state.Record();

		await state.UpdateDataAsync(_ => default(string)!, CT);

		result.Should().Be(m => m
			.Message("42")
			.Message(default(string)!)
		);
	}

	[TestMethod]
	public async Task When_UpdateDataAsyncReturnsDefaultOptionStateOfNullableString_Then_TreatAsNone()
	{
		var state = State<string?>.Value(this, () => "42");
		var result = state.Record();

		await state.UpdateDataAsync(_ => default(Option<string?>), CT);

		result.Should().Be(m => m
			.Message("42")
			.Message(Data.None)
		);
	}

	[TestMethod]
	public async Task When_UpdateDataAsyncReturnsNullStateOfNullableString_Then_TreatAsSome()
	{
		var state = State<string?>.Value(this, () => "42");
		var result = state.Record();

		await state.UpdateDataAsync(_ => default(string?), CT);

		result.Should().Be(m => m
			.Message("42")
			.Message(Option.Some(default(string?)))
		);
	}

	[TestMethod]
	public async Task When_UpdateDataAsyncReturnsDefaultOptionStateOfStruct_Then_TreatAsNone()
	{
		var state = State<MyStruct>.Value(this, () => new MyStruct());
		var result = state.Record();

		await state.UpdateDataAsync(_ => default(Option<MyStruct>), CT);

		result.Should().Be(m => m
			.Message(Data.Some)
			.Message(Data.None)
		);
	}

	[TestMethod]
	public async Task When_UpdateDataAsyncReturnsDefaultValueStateOfStruct_Then_TreatAsSome()
	{
		var state = State<MyStruct>.Value(this, () => new MyStruct());
		var result = state.Record();

		await state.UpdateDataAsync(_ => default(MyStruct), CT);

		result.Should().Be(m => m
			.Message(Data.Some)
			// .Message(Data.Some) // Nothing changed!
		);
	}

	[TestMethod]
	public async Task When_UpdateDataAsyncReturnsDefaultOptionStateOfNullableStruct_Then_TreatAsNone()
	{
		var state = State<MyStruct?>.Value(this, () => new MyStruct());
		var result = state.Record();

		await state.UpdateDataAsync(_ => default(Option<MyStruct?>), CT);

		result.Should().Be(m => m
			.Message(Data.Some)
			.Message(Data.None)
		);
	}

	[TestMethod]
	public async Task When_UpdateDataAsyncReturnsNullStateOfNullableStruct_Then_TreatAsSome()
	{
		var state = State<MyStruct?>.Value(this, () => new MyStruct());
		var result = state.Record();

		await state.UpdateDataAsync(_ => default(MyStruct?), CT);

		result.Should().Be(m => m
			.Message(Data.Some)
			.Message(Option.Some(default(MyStruct?)))
		);
	}

	[TestMethod]
	public async Task When_UpdateDataAsyncReturnsDefaultOptionStateOfObject_Then_TreatAsNone()
	{
		var state = State<MyClass>.Value(this, () => new MyClass());
		var result = state.Record();

		await state.UpdateDataAsync(_ => default(Option<MyClass>), CT);

		result.Should().Be(m => m
			.Message(Data.Some)
			.Message(Data.None)
		);
	}

	[TestMethod]
	public async Task When_UpdateDataAsyncReturnsDefaultValueStateOfObject_Then_TreatAsSome()
	{
		var state = State<MyClass>.Value(this, () => new MyClass());
		var result = state.Record();

		await state.UpdateDataAsync(_ => default(MyClass)!, CT);

		result.Should().Be(m => m
			.Message(Data.Some)
			.Message(default(MyClass)!)
		);
	}

	[TestMethod] // Forbidden, need to use UpdateDataAsync instead
	public async Task When_UpdateDataAsyncReturnsDefaultOptionStateOfNullableObject_Then_TreatAsNone()
	{
		var state = State<MyClass?>.Value(this, () => new MyClass());
		var result = state.Record();

		await state.UpdateDataAsync(_ => default(Option<MyClass?>), CT);

		result.Should().Be(m => m
			.Message(Data.Some)
			.Message(Data.None)
		);
	}

	[TestMethod] // Forbidden, need to use UpdateDataAsync instead
	public async Task When_UpdateDataAsyncReturnsNullStateOfNullableObject_Then_TreatAsSome()
	{
		var state = State<MyClass?>.Value(this, () => new MyClass());
		var result = state.Record();

		await state.UpdateDataAsync(_ => default(MyClass?), CT);

		result.Should().Be(m => m
			.Message(Data.Some)
			.Message(Option.Some(default(MyClass?)))
		);
	}
	#endregion

	#region SetAsync
	[TestMethod]
	// TODO: Expect null ref exception
	public async Task When_SetAsync_Then_AcceptsNotNullAndStruct()
	{
		// Note: This is a compilation tests!
		await default(IState<int>)!.SetAsync(42, CT);
		await default(IState<int>)!.SetAsync(null, CT);

		await default(IState<int?>)!.SetAsync(42, CT);
		await default(IState<int?>)!.SetAsync(null, CT);

		await default(IState<string>)!.SetAsync("", CT);
		await default(IState<string>)!.SetAsync(null, CT);

		await default(IState<string?>)!.SetAsync("", CT);
		await default(IState<string?>)!.SetAsync(null, CT);

		await default(IState<MyStruct>)!.SetAsync(new MyStruct(), CT);
		await default(IState<MyStruct?>)!.SetAsync(null, CT);

		// Forbidden for ACID, must use UpdateAsync instead
		//await default(IState<MyClass>)!.SetAsync(new MyClass(), CT);
		//await default(IState<MyClass?>)!.SetAsync(default(MyClass?), CT);
	}

	[TestMethod]
	public async Task When_SetAsyncNullStateOfInt_Then_TreatAsNone()
	{
		var state = State<int>.Value(this, () => 42);
		var result = state.Record();

		await state.SetAsync(null, CT);

		result.Should().Be(m => m
			.Message(42)
			.Message(Data.None)
		);
	}

	[TestMethod]
	public async Task When_SetAsyncNullStateOfNullableInt_Then_TreatAsNone()
	{
		var state = State<int?>.Value(this, () => 42);
		var result = state.Record();

		await state.SetAsync(null, CT);

		result.Should().Be(m => m
			.Message(42)
			.Message(Data.None)
		);
	}

	[TestMethod]
	public async Task When_SetAsyncNullStateOfString_Then_TreatAsNone()
	{
		var state = State<string>.Value(this, () => "42");
		var result = state.Record();

		await state.SetAsync(null, CT);

		result.Should().Be(m => m
			.Message("42")
			.Message(Data.None)
		);
	}

	//[TestMethod] // Forbidden, need to use UpdateDataAsync instead
	//public async Task When_SetAsyncNullStateOfNullableString_Then_TreatAsNone()
	//{
	//	var state = State<string?>.Value(this, () => "42");
	//	var result = state.Record();

	//	await state.SetAsync(null, CT);

	//	result.Should().Be(m => m
	//		.Message("42")
	//		.Message(Data.None)
	//	);
	//}

	[TestMethod]
	public async Task When_SetAsyncNullStateOfStruct_Then_TreatAsNone()
	{
		var state = State<MyStruct>.Value(this, () => new MyStruct());
		var result = state.Record();

		await state.SetAsync(null, CT);

		result.Should().Be(m => m
			.Message(Data.Some)
			.Message(Data.None)
		);
	}

	[TestMethod]
	public async Task When_SetAsyncNullStateOfNullableStruct_Then_TreatAsNone()
	{
		var state = State<MyStruct?>.Value(this, () => new MyStruct());
		var result = state.Record();

		await state.SetAsync(null, CT);

		result.Should().Be(m => m
			.Message(Data.Some)
			.Message(Data.None)
		);
	}

	//[TestMethod] // Forbidden, need to use UpdateDataAsync instead
	//public async Task When_SetAsyncNullStateOf<Nullable>Object_Then_TreatAsNone()
	//{
	//	var state = State.Value(this, () => new MyClass());
	//	var result = state.Record();

	//	await state.SetAsync(null, CT);

	//	result.Should().Be(m => m
	//		.Message(Data.Some)
	//		.Message(Data.None));
	//} 
	#endregion

	private record class MyClass;
	private record struct MyStruct;
}
